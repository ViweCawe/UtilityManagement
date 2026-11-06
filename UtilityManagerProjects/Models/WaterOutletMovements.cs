using DataLibrary.Models;

namespace UtilityManagerProjects.Models;

public sealed class WaterOutletMovement
{
    public int MeterId { get; init; }
    public string MeterName { get; init; } = "";
    public string Point { get; init; } = "";
    public string Destinations { get; init; } = "";
    public int OpeningId { get; init; }
    public int ClosingId { get; init; }
    public DateTime OpeningDate { get; init; }
    public DateTime ClosingDate { get; init; }
    public decimal OpeningCounter { get; init; }
    public decimal ClosingCounter { get; init; }
    public decimal? Volume { get; init; }
    public string CounterUnit { get; init; } = "L";
    public decimal DisplayFactor => MeterUnits.Valid(CounterUnit,MeterType.Water)?MeterUnits.Factor(CounterUnit,MeterType.Water):1m;
    public decimal DisplayOpeningCounter => OpeningCounter*DisplayFactor;
    public decimal DisplayClosingCounter => ClosingCounter*DisplayFactor;
    public string DisplayUnit => MeterUnits.Valid(CounterUnit,MeterType.Water)?"L":CounterUnit;
    public string Status { get; init; } = "";
}

public sealed class WaterOutletMovementTable
{
    public List<WaterOutletMovement> Rows { get; init; } = [];
    public bool Truncated { get; init; }
}

public static class WaterOutletMovements
{
    // This is evidence between captures, including intraday captures. It is not
    // a second daily-usage allocator and does not extrapolate a full-period total.
    public static WaterOutletMovementTable Calculate(WaterNetwork map, IReadOnlyDictionary<string,int?> links,
        IEnumerable<Meter> meters, IEnumerable<MeterReading> captures, DateTime start, DateTime end, int? meterId=null, string? department=null)
    {
        start=start.Date;end=end.Date;
        var active=meters.Where(m=>m.IsActive && m.MeterType==MeterType.Water && (!meterId.HasValue||m.Id==meterId) &&
            (string.IsNullOrWhiteSpace(department)||string.Equals(m.DepartmentName.Trim(),department.Trim(),StringComparison.OrdinalIgnoreCase))).ToDictionary(m=>m.Id);
        var final=WaterConsumptionBoundaries.Resolve(map,links,meters).MeterIds;
        var pointByMeter=links.Where(p=>p.Value.HasValue&&active.ContainsKey(p.Value.Value)&&map.Nodes.Any(n=>n.Id==p.Key&&n.CanLinkMeter))
            .GroupBy(p=>p.Value!.Value).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>map.Nodes.Single(n=>n.Id==g.Single().Key));
        var rows=new List<WaterOutletMovement>();
        foreach(var group in captures.Where(r=>!r.IsDeleted&&active.ContainsKey(r.MeterId)&&r.ReadingDate.Date<=DateTime.Today).GroupBy(r=>r.PhysicalMeterId ?? r.MeterId))
        {
            var reportingId=group.First().MeterId;
            if(final.Contains(reportingId)||!pointByMeter.TryGetValue(reportingId,out var point)||WaterAccounting.CounterBoundary(point)!="outlet")continue;
            var destinations=new HashSet<string>();var seen=new HashSet<string>();
            var queue=new Queue<string>(map.Connections.Where(e=>e.From==point.Id).Select(e=>e.To));
            while(queue.TryDequeue(out var id))
            {
                if(!seen.Add(id))continue;
                var next=map.Nodes.Single(n=>n.Id==id);
                if(next.CanLinkMeter||next.Kind=="storage")destinations.Add(next.Label);
                else foreach(var edge in map.Connections.Where(e=>e.From==id))queue.Enqueue(edge.To);
            }
            var ordered=group.OrderBy(r=>r.ReadingDate).ThenBy(r=>r.Id).ToList();
            for(var i=1;i<ordered.Count;i++)
            {
                var previous=ordered[i-1];var current=ordered[i];
                if(current.ReadingDate.Date<start||current.ReadingDate.Date>end)continue;
                var delta=(decimal)current.CurrentReading-previous.CurrentReading;
                var notes=new List<string>();
                if(delta<0)notes.Add("Counter decreased: verify reset/correction; no negative transfer is reported.");
                if(previous.ReadingDate.Date<start)notes.Add("Entire capture interval shown; opening capture predates the date filter.");
                if(previous.ReadingDate.Date==current.ReadingDate.Date)notes.Add("Same-day capture change; daily-average coverage remains separate.");
                if(destinations.Count!=1)notes.Add("Destination volume is not split across branches. Use a dedicated transfer measurement or confirmed filling record to identify a specific destination.");
                else notes.Add("Measured at the sender's outlet. Arrival at the next tank is not separately measured here.");
                rows.Add(new(){MeterId=reportingId,MeterName=group.First().MeterName,Point=point.Label,Destinations=string.Join(", ",destinations),
                    OpeningId=previous.Id,ClosingId=current.Id,OpeningDate=previous.ReadingDate,ClosingDate=current.ReadingDate,
                    OpeningCounter=previous.CurrentReading,ClosingCounter=current.CurrentReading,CounterUnit=current.Unit,Volume=delta>=0&&current.Unit==previous.Unit&&MeterUnits.Valid(current.Unit,current.MeterType)?delta*MeterUnits.Factor(current.Unit,current.MeterType):null,Status=string.Join(" ",notes)});
            }
        }
        return new(){Rows=rows.OrderByDescending(r=>r.ClosingDate).ThenBy(r=>r.MeterName).Take(500).ToList(),Truncated=rows.Count>500};
    }
}
