using DataLibrary.Data;
using DataLibrary.Db;
using DataLibrary.Models;
using UtilityManagerProjects.Models;

namespace UtilityManagerProjects.Services;

public sealed class ConsumptionService(IMeterReadingData readings, IMeterData meterData, WaterNetworkStore store,
    IDataAccess db, ConnectionStringData connection, IConfiguration configuration)
{
    public sealed class Link { public string NodeId { get; set; } = ""; public int? MeterId { get; set; } }
    private Task? load;
    private readonly Dictionary<(DateTime,DateTime,int?,string),ConsumptionPeriod> periods = [];
    public List<MeterReading> History { get; private set; } = [];
    public List<Meter> Meters { get; private set; } = [];
    public List<string> Warnings { get; } = [];
    private WaterNetwork network = new();
    private Dictionary<string, int?> links = [];

    public Task LoadAsync() => load ??= LoadCoreAsync();
    private async Task LoadCoreAsync()
    {
        Meters = (await meterData.GetMeters()).Where(m => m.IsActive && m.MeterType is MeterType.Water or MeterType.Electricity).ToList();
        var ids = Meters.Select(m => m.Id).ToHashSet();
        History = (await readings.GetAllMeterReading()).Where(r => !r.IsDeleted && ids.Contains(r.MeterId) && r.ReadingDate.Date <= DateTime.Today).ToList();
        network = (await store.LoadAsync()).Network;
        links = configuration.GetSection("WaterNetwork:MeterIds").GetChildren()
            .ToDictionary(p => p.Key, p => int.TryParse(p.Value, out var id) ? (int?)id : null);
        foreach (var link in await db.LoadData<Link, object>("dbo.spWaterNetworkLinks_All", new { }, connection.SqlConnectionName)) links[link.NodeId] = link.MeterId;
        if (History.GroupBy(r => r.MeterId).Any(g => g.OrderBy(r => r.ReadingDate).ThenBy(r => r.Id).Zip(g.OrderBy(r => r.ReadingDate).ThenBy(r => r.Id).Skip(1)).Any(p => p.Second.CurrentReading < p.First.CurrentReading)))
            Warnings.Add("A counter decreases in the reading history. That interval is excluded until the reset or incorrect capture is resolved.");
    }

    public List<MeterDailyAverageRow> DailyAverages(ConsumptionPeriod period,int? meterId=null,string? department=null) => Meters
        .Where(m=>(!meterId.HasValue||m.Id==meterId) && (string.IsNullOrWhiteSpace(department)||string.Equals(m.DepartmentName,department,StringComparison.OrdinalIgnoreCase)))
        .Select(m=>{
            var days=period.AllDaily.Where(d=>d.MeterId==m.Id).ToList();
            var captures=History.Where(r=>r.MeterId==m.Id).ToList();
            var latest=captures.Where(r=>r.ReadingDate.Date<=period.Water.End).OrderByDescending(r=>r.ReadingDate).ThenByDescending(r=>r.Id).FirstOrDefault();
            var status=days.Count>0?"Usage calculated":latest is null?"No saved counter by end date":captures.Select(r=>r.ReadingDate.Date).Distinct().Count()<2?"Starting counter saved — capture another reading on a later date":"No comparable interval for selected dates";
            var points=network.Nodes.Where(n=>links.GetValueOrDefault(n.Id)==m.Id).ToList();
            var meterOrigins=points.SelectMany(n=>WaterAccounting.Origins(network,n.Id)).Distinct().ToList();
            var supply=m.MeterType!=MeterType.Water?"Electricity":meterOrigins.Count>1?"Combined sources · split not separately measured":meterOrigins.SingleOrDefault() switch {"municipal"=>"Municipal supply","borehole"=>"Borehole supply","rain"=>"Rainwater",_=>"Water source not confirmed on map"};
            return new MeterDailyAverageRow {SupplyDescription=supply,LatestCounter=latest?.CurrentReading,LatestCounterDate=latest?.ReadingDate,CalculationStatus=status,MeterId=m.Id,MeterName=m.MeterName,MeterType=m.MeterType,DepartmentName=m.DepartmentName,
                ReadingCount=History.Count(r=>r.MeterId==m.Id&&r.ReadingDate.Date>=period.Water.Start&&r.ReadingDate.Date<=period.Water.End),
                CoveredDays=days.Count,TotalUsage=days.Sum(d=>d.Usage),DailyAverage=days.Count==0?0:days.Sum(d=>d.Usage)/days.Count};
        }).OrderBy(r=>r.DepartmentName).ThenBy(r=>r.MeterName).ToList();

    public WaterStorageReport StorageForPeriod(DateTime start,DateTime end) => WaterStorageAccounting.Calculate(network,links,Meters,History,start,end);

    public static (DateTime Start, DateTime End, string? Notice) ReportingRange(DateTime start, DateTime end)
    {
        var originalStart=start.Date; var originalEnd=end.Date;
        start=start.Date; end=end.Date;
        if(start>end) (start,end)=(end,start);
        var floor=new DateTime(1753,1,1); var today=DateTime.Today;
        if(start<floor) start=floor;
        if(end<floor) end=floor;
        if(end>today) end=today;
        if(start>end) start=end;
        if((end-start).Days>3660) start=end.AddDays(-3660);
        return(start,end,start!=originalStart||end!=originalEnd?"Report dates were adjusted to supported dates: at most ten years, ending no later than today. See the displayed reporting period.":null);
    }

    public ConsumptionPeriod ForPeriod(DateTime start, DateTime end, int? meterId = null, string? department = null)
    {
        start=start.Date; end=end.Date;
        department=string.IsNullOrWhiteSpace(department)?null:department.Trim();
        var key=(start,end,meterId,department?.ToUpperInvariant()??"");
        if(periods.TryGetValue(key,out var cached)) return cached;
        if (end.Date < start.Date || (end.Date - start.Date).Days > 3660) throw new ArgumentException("Choose a date range of at most ten years.");
        var selected = Meters.Where(m => (!meterId.HasValue || m.Id == meterId) &&
            (string.IsNullOrWhiteSpace(department) || string.Equals(m.DepartmentName.Trim(), department, StringComparison.OrdinalIgnoreCase))).ToList();
        var selectedIds = selected.Select(m => m.Id).ToHashSet();
        var daily = IntervalConsumption.Allocate(History.Where(r => selectedIds.Contains(r.MeterId)), start, end);
        var water = selected.Where(m => m.MeterType == MeterType.Water).Select(m => m.Id).ToHashSet();
        var expected = new HashSet<int>(); var missing = new List<string>();
        var validLinks = links.Where(p => p.Value.HasValue && water.Contains(p.Value.Value) && network.Nodes.Any(n => n.Id == p.Key && n.CanLinkMeter)).ToDictionary(p => p.Key, p => p.Value!.Value);
        if (!meterId.HasValue && validLinks.Values.GroupBy(id=>id).Any(g=>g.Count()>1)) missing.Add("A water meter is mapped to multiple network points. Correct the assignments before using aggregate totals.");
        var allEdges = network.Connections; // Even conditional routes imply possible overlap; never add a parent and its child.
        bool HasSelectedAncestor(string node)
        {
            var queue = new Queue<string>(allEdges.Where(e => e.To == node).Select(e => e.From)); var seen = new HashSet<string>();
            while (queue.TryDequeue(out var id))
            {
                if (!seen.Add(id)) continue;
                if (validLinks.ContainsKey(id)) return true;
                foreach (var e in allEdges.Where(e => e.To == id)) queue.Enqueue(e.From);
            }
            return false;
        }
        bool HasMunicipalAncestor(string node,bool selectedOnly)
        {
            var queue=new Queue<string>(allEdges.Where(e=>e.To==node&&e.SourceFilter is not ("borehole" or "rain" or "other")).Select(e=>e.From));var seen=new HashSet<string>();
            while(queue.TryDequeue(out var id)) {
                if(!seen.Add(id)) continue;
                var point=network.Nodes.Single(n=>n.Id==id);
                if(selectedOnly?validLinks.ContainsKey(id)&&WaterAccounting.Origins(network,id).SetEquals(new[]{"municipal"}):WaterAccounting.Role(point)=="main") return true;
                foreach(var edge in allEdges.Where(e=>e.To==id&&e.SourceFilter is not ("borehole" or "rain" or "other"))) queue.Enqueue(edge.From);
            }
            return false;
        }
        var label = "Main Meter — municipal supply";
        if (meterId.HasValue) { expected = water; label = "Estimated flow through the selected meter"; }
        else if (!string.IsNullOrWhiteSpace(department))
        {
            label = "Estimated department flow (topmost linked meters; department totals may overlap)";
            foreach (var link in validLinks.Where(p => !HasSelectedAncestor(p.Key) && (WaterAccounting.Role(network.Nodes.Single(n=>n.Id==p.Key))=="main" || HasMunicipalAncestor(p.Key,false)) && WaterAccounting.Origins(network,p.Key).SetEquals(new[]{"municipal"}))) expected.Add(link.Value);
            label="Main branch municipal flow for selected department";
            foreach(var link in validLinks.Where(p=>WaterAccounting.Origins(network,p.Key).Count>1 && !HasMunicipalAncestor(p.Key,true))) missing.Add($"{network.Nodes.Single(n=>n.Id==link.Key).Label}: combined-source meter; municipal department flow cannot be separated.");
            foreach (var id in water.Except(validLinks.Values)) missing.Add($"Unmapped meter #{id}");
        }
        else
        {
            var mains=network.Nodes.Where(n=>WaterAccounting.Role(n)=="main").ToList();
            if(mains.Count!=1) missing.Add("Choose one Main Meter reporting boundary in the network editor.");
            foreach(var node in mains) {
                if(validLinks.TryGetValue(node.Id,out var id)) expected.Add(id);
                else missing.Add($"Assign the actual Main Meter: {node.Label}");
            }
        }
        var electricIds = selected.Where(m => m.MeterType == MeterType.Electricity).Select(m => m.Id).ToHashSet();
        ConsumptionScope MeterScope(IEnumerable<WaterNetworkNode> nodes,string scopeLabel,bool frontier=false,HashSet<DateTime>? coveredDates=null)
        {
            var candidates=nodes.Where(n=>n.CanLinkMeter).ToList();
            if(frontier) candidates=candidates.Where(n=>!HasAncestorIn(n.Id,candidates.Select(c=>c.Id).ToHashSet())).ToList();
            var ids=new HashSet<int>();var gaps=new List<string>();
            foreach(var node in candidates) { if(validLinks.TryGetValue(node.Id,out var id)) ids.Add(id);else gaps.Add($"Assign the meter: {node.Label}"); }
            return new(){Start=start,End=end,MeterIds=ids,Missing=gaps,Allocated=coveredDates is null?daily:daily.Where(d=>coveredDates.Contains(d.ReadingDate)).ToList(),Label=scopeLabel};
        }
        bool HasAncestorIn(string id,HashSet<string> candidates)
        {
            var queue=new Queue<string>(allEdges.Where(e=>e.To==id).Select(e=>e.From));var seen=new HashSet<string>();
            while(queue.TryDequeue(out var parent)) { if(!seen.Add(parent)) continue;if(candidates.Contains(parent)) return true;foreach(var e in allEdges.Where(e=>e.To==parent)) queue.Enqueue(e.From); }
            return false;
        }
        var reportLabels=new Dictionary<string,string>{{"main","Main Meter · municipal supply"},{"borehole-outlet","Borehole Out · water leaving storage"},{"borehole-inlet","Borehole Incoming · water entering storage"},{"hydrant","Fire Hydrant · separate municipal branch"}};
        var supplies=network.Nodes.Where(n=>WaterAccounting.Role(n).Length>0 && (!meterId.HasValue&&!string.IsNullOrWhiteSpace(department)?validLinks.ContainsKey(n.Id):!meterId.HasValue||validLinks.ContainsKey(n.Id)))
            .Select(n=>new WaterSupplyRow{Role=WaterAccounting.Role(n),Label=reportLabels[WaterAccounting.Role(n)],Flow=MeterScope(new[]{n},n.Label)}).OrderBy(n=>n.Role=="main"?0:n.Role=="borehole-outlet"?1:n.Role=="hydrant"?2:3).ToList();
        var areaNodes=network.Nodes.Where(n=>WaterAccounting.Area(n).Length>0 && (meterId.HasValue||!string.IsNullOrWhiteSpace(department)?validLinks.ContainsKey(n.Id):true)).ToList();
        var areas=areaNodes.GroupBy(WaterAccounting.Area,StringComparer.OrdinalIgnoreCase).Select(group=> {
            var combined=MeterScope(group,group.Key,true);
            var covered=combined.CompleteRows.Select(d=>d.ReadingDate).ToHashSet();
            return new WaterAreaRow {
                Area=group.Key,HasMunicipalSupply=group.Any(n=>WaterAccounting.Origins(network,n.Id).Contains("municipal")),HasBoreholeSupply=group.Any(n=>WaterAccounting.Origins(network,n.Id).Contains("borehole")),
                HasCombinedMeter=group.Any(n=>n.CanLinkMeter&&WaterAccounting.Origins(network,n.Id).Count>1),Combined=combined,
                Municipal=MeterScope(group.Where(n=>WaterAccounting.Origins(network,n.Id).SetEquals(new[]{"municipal"})),group.Key+" municipal",true,covered),
                Borehole=MeterScope(group.Where(n=>WaterAccounting.Origins(network,n.Id).SetEquals(new[]{"borehole"})),group.Key+" borehole",true,covered)
            };
        }).ToList();
        var captured=History.Where(r=>selectedIds.Contains(r.MeterId)&&r.ReadingDate.Date>=start&&r.ReadingDate.Date<=end).ToList();
        var capturedIds=captured.Select(r=>r.MeterId).ToHashSet();
        var startingOnly=capturedIds.Count(id=>History.Where(r=>r.MeterId==id).Select(r=>r.ReadingDate.Date).Distinct().Count()<2);
        var result = new ConsumptionPeriod { WaterSupplies=supplies,WaterAreas=areas,AllDaily = daily,CapturesInPeriod=captured.Count,StartingOnlyMeterCount=startingOnly,
            Water = new ConsumptionScope { Start = start, End = end, MeterIds = expected, Missing = missing, Allocated = daily, Label = label },
            Electricity = new ConsumptionScope { Start = start, End = end, MeterIds = electricIds, Allocated = daily, Label = "Estimated electricity flow through selected meters" } };
        foreach(var scope in new[]{result.Water,result.Electricity})
        {
            foreach(var id in scope.MeterIds.Where(id=>!daily.Any(d=>d.MeterId==id)))
            {
                var meter=Meters.Single(m=>m.Id==id);
                var captures=History.Where(r=>r.MeterId==id).ToList();
                scope.CoverageIssues.Add(captures.Count==0?$"{meter.MeterName}: no saved readings.":
                    captures.Select(r=>r.ReadingDate.Date).Distinct().Count()<2?$"{meter.MeterName}: starting counter saved; another reading on a later date is needed.":
                    $"{meter.MeterName}: no valid reading interval overlaps the selected dates.");
            }
            if(scope.MeterIds.Count>0&&scope.Missing.Count==0&&scope.CoveredDays==0&&scope.CoverageIssues.Count==0)
                scope.CoverageIssues.Add("These meters have readings, but their covered dates do not overlap. Select one meter to view its flow, or capture all supply meters over matching dates.");
        }
        periods[key]=result;
        return result;
    }
}
