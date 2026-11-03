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
            return new MeterDailyAverageRow {MeterId=m.Id,MeterName=m.MeterName,MeterType=m.MeterType,DepartmentName=m.DepartmentName,
                ReadingCount=History.Count(r=>r.MeterId==m.Id&&r.ReadingDate.Date>=period.Water.Start&&r.ReadingDate.Date<=period.Water.End),
                CoveredDays=days.Count,TotalUsage=days.Sum(d=>d.Usage),DailyAverage=days.Count==0?0:days.Sum(d=>d.Usage)/days.Count};
        }).OrderBy(r=>r.DepartmentName).ThenBy(r=>r.MeterName).ToList();

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
        var label = "Estimated incoming water flow (external supply boundary)";
        if (meterId.HasValue) { expected = water; label = "Estimated flow through the selected meter"; }
        else if (!string.IsNullOrWhiteSpace(department))
        {
            label = "Estimated department flow (topmost linked meters; department totals may overlap)";
            foreach (var link in validLinks.Where(p => !HasSelectedAncestor(p.Key))) expected.Add(link.Value);
            foreach (var id in water.Except(validLinks.Values)) missing.Add($"Unmapped meter #{id}");
        }
        else
        {
            var roots = network.Nodes.Where(n => n.CanLinkMeter && (n.IsExternalSupply ?? (n.Id is "main-tank" or "fire-hydrant" or "borehole-in"))).ToList();
            foreach(var meter in selected.Where(m=>m.MeterType==MeterType.Water && m.NetworkRole==WaterNetworkRole.Source))
                if(!validLinks.Values.Contains(meter.Id)) missing.Add($"Source meter {meter.MeterName} is not linked to the network. Assign it and confirm whether it is external supply.");
            if (roots.Count == 0) missing.Add("No external supply boundary is configured in the network.");
            foreach (var node in roots)
            {
                if(network.Connections.Any(e=>e.To==node.Id&&e.Kind!="backup")) { missing.Add($"External supply {node.Label} has a normal upstream connection. Correct the reporting boundary."); continue; }
                if (validLinks.TryGetValue(node.Id, out var id)) expected.Add(id);
                else missing.Add($"Assign the source meter: {node.Label}");
            }
        }
        var electricIds = selected.Where(m => m.MeterType == MeterType.Electricity).Select(m => m.Id).ToHashSet();
        var result = new ConsumptionPeriod { AllDaily = daily,
            Water = new ConsumptionScope { Start = start, End = end, MeterIds = expected, Missing = missing, Allocated = daily, Label = label },
            Electricity = new ConsumptionScope { Start = start, End = end, MeterIds = electricIds, Allocated = daily, Label = "Estimated electricity flow through selected meters" } };
        periods[key]=result;
        return result;
    }
}
