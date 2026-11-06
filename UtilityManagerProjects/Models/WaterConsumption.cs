using DataLibrary.Models;

namespace UtilityManagerProjects.Models;

public sealed class WaterConsumptionSelection
{
    public HashSet<int> MeterIds { get; init; } = [];
    public List<string> Issues { get; init; } = [];
}

public sealed class WaterConsumptionTable
{
    public List<MeterDailyAverageRow> Rows { get; init; } = [];
    public ConsumptionScope Scope { get; init; } = new();
}

public static class WaterConsumptionBoundaries
{
    // A cumulative difference is consumption only at a confirmed final-use boundary.
    // EndUse is a legacy default in the meter registry, so it alone is insufficient.
    public static WaterConsumptionSelection Resolve(WaterNetwork map, IReadOnlyDictionary<string, int?> links, IEnumerable<Meter> meters)
    {
        var active = meters.Where(m => m.IsActive && m.MeterType == MeterType.Water).Select(m => m.Id).ToHashSet();
        var credited = map.Nodes.Where(n => n.Storage is not null).SelectMany(n => n.Storage!.InletMeterIds).ToHashSet();
        var duplicateIds = links.Where(p => p.Value.HasValue && map.Nodes.Any(n => n.Id == p.Key))
            .GroupBy(p => p.Value!.Value).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        var ids = new HashSet<int>();
        var issues = new List<string>();
        foreach (var node in map.Nodes.Where(n => n.MeasuresFinalUse))
        {
            var meterId = links.GetValueOrDefault(node.Id);
            if (!node.CanLinkMeter || !meterId.HasValue || !active.Contains(meterId.Value))
            { issues.Add($"Link an active final-use meter: {node.Label}."); continue; }
            var descendants = new HashSet<string>();
            var queue = new Queue<string>(map.Connections.Where(e => e.From == node.Id).Select(e => e.To));
            while (queue.TryDequeue(out var id))
                if (descendants.Add(id)) foreach (var edge in map.Connections.Where(e => e.From == id)) queue.Enqueue(edge.To);
            if (WaterAccounting.Role(node) is "main" or "municipal-inlet" or "borehole-inlet" or "borehole-outlet" ||
                credited.Contains(meterId.Value) || duplicateIds.Contains(meterId.Value) ||
                (node.Kind == "storage" && WaterAccounting.CounterBoundary(node) != "outlet") ||
                map.Nodes.Any(n => descendants.Contains(n.Id) && (n.CanLinkMeter || n.Kind == "storage")))
            { issues.Add($"{node.Label}: a source/transfer/storage-inlet or overlapping counter cannot count as final consumption."); continue; }
            ids.Add(meterId.Value);
        }
        return new() { MeterIds = ids, Issues = issues };
    }
}
