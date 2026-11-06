using DataLibrary.Models;

namespace UtilityManagerProjects.Models;

public sealed class WaterSourceRoute
{
    public string SourceId { get; init; } = "";
    public string Origin { get; init; } = "";
    public string SourceLabel { get; init; } = "";
    public string Path { get; init; } = "";
    public bool Conditional { get; init; }
}

public sealed class WaterFlowPoint
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Station { get; init; } = "";
    public string Kind { get; init; } = "";
    public string ReportingRole { get; init; } = "";
    public int Depth { get; init; }
    public int? MeterId { get; init; }
    public string MeterName { get; init; } = "";
    public bool MeasuresFinalUse { get; init; }
    public ConsumptionScope Flow { get; init; } = new();
    public List<WaterSourceRoute> Sources { get; init; } = [];
    public List<string> NextPoints { get; init; } = [];
    public List<string> ComparisonIssues { get; init; } = [];
    public int ComparedDays { get; set; }
    public decimal? ComparedParent { get; set; }
    public decimal? ComparedBranches { get; set; }
    public decimal? Difference => ComparedParent - ComparedBranches;
}

public sealed class WaterStationFlow
{
    public string Station { get; init; } = "";
    public List<WaterFlowPoint> Points { get; init; } = [];
}

public sealed class WaterStationTransfer
{
    public string FromId { get; init; } = "";
    public string ToId { get; init; } = "";
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public string FromStation { get; init; } = "";
    public string ToStation { get; init; } = "";
    public bool ExclusiveRoute { get; init; }
    public ConsumptionScope Received { get; init; } = new();
    public ConsumptionScope Sent { get; init; } = new();
    public bool DedicatedDeparture { get; init; }
    public string MeasurementNote { get; init; } = "";
}

public sealed class BoreholeFlowPair
{
    public ConsumptionScope Incoming { get; init; } = new();
    public ConsumptionScope Outgoing { get; init; } = new();
    public decimal? MatchedIncoming { get; set; }
    public decimal? MatchedOutgoing { get; set; }
    public decimal? Difference => MatchedIncoming - MatchedOutgoing;
    public int MatchedDays { get; set; }
    public List<string> Issues { get; init; } = [];
}

public sealed class WaterFlowTraceReport
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public List<WaterStationFlow> Stations { get; init; } = [];
    public List<WaterStationTransfer> Transfers { get; init; } = [];
    public BoreholeFlowPair Borehole { get; init; } = new();
    public ConsumptionScope MunicipalReceipt { get; init; } = new();
    public List<string> Warnings { get; init; } = [];
    public IEnumerable<WaterFlowPoint> Points => Stations.SelectMany(s => s.Points);
}

public static class WaterFlowTracing
{
    public static WaterFlowTraceReport Calculate(WaterNetwork map, IReadOnlyDictionary<string, int?> links,
        List<Meter> meters, List<MeterReading> history, DateTime start, DateTime end)
    {
        start = start.Date;
        end = end.Date;
        if (end < start || (end - start).Days > 3660)
            throw new ArgumentException("Choose a date range of at most ten years.");
        var nodes = map.Nodes.ToDictionary(n => n.Id);
        var incoming = map.Nodes.ToDictionary(n => n.Id, n => map.Connections.Where(e => e.To == n.Id).ToList());
        var outgoing = map.Nodes.ToDictionary(n => n.Id, n => map.Connections.Where(e => e.From == n.Id).ToList());
        var active = meters.Where(m => m.IsActive && m.MeterType == MeterType.Water).ToDictionary(m => m.Id);
        var valid = links.Where(p => nodes.TryGetValue(p.Key, out var n) && n.CanLinkMeter && p.Value.HasValue && active.ContainsKey(p.Value.Value))
            .ToDictionary(p => p.Key, p => p.Value!.Value);
        var duplicateIds = valid.Values.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        var warnings = duplicateIds.Select(id => $"Meter #{id} is assigned more than once. Correct its links before comparing branches.").ToList();
        foreach (var id in valid.Where(p => duplicateIds.Contains(p.Value)).Select(p => p.Key).ToList()) valid.Remove(id);
        var daily = IntervalConsumption.Allocate(history.Where(h => active.ContainsKey(h.MeterId) && h.ReadingDate.Date <= DateTime.Today), start, end);

        var dailyByMeter = daily.GroupBy(d => d.MeterId).ToDictionary(g => g.Key, g => g.ToList());

        ConsumptionScope Scope(IEnumerable<string> pointIds)
        {
            var ids = pointIds.Distinct().ToList();
            var counterIds = ids.Where(valid.ContainsKey).Select(id => valid[id]).ToHashSet();
            return new()
            {
                Start = start, End = end, Allocated = counterIds.SelectMany(id => dailyByMeter.GetValueOrDefault(id) ?? []).ToList(),
                MeterIds = counterIds,
                Missing = ids.Where(id => !valid.ContainsKey(id)).Select(id => $"Link an active water meter: {nodes[id].Label}.").ToList()
            };
        }
        decimal SumOnDays(int meterId, HashSet<DateTime> dates) => (dailyByMeter.GetValueOrDefault(meterId) ?? []).Where(d => dates.Contains(d.ReadingDate)).Sum(d => d.Usage);

        // Stop at physical meter boundaries even if the meter has not been linked.
        // Skipping an unlinked parent would double-count its downstream counters.
        HashSet<string> PreviousBoundaries(string id)
        {
            var result = new HashSet<string>();
            var seen = new HashSet<string>();
            var queue = new Queue<string>(incoming[id].Select(e => e.From));
            while (queue.TryDequeue(out var previous))
            {
                if (!seen.Add(previous)) continue;
                if (nodes[previous].CanLinkMeter || incoming[previous].Count == 0) result.Add(previous);
                else foreach (var edge in incoming[previous]) queue.Enqueue(edge.From);
            }
            return result;
        }
        (HashSet<string> Meters, HashSet<string> UnmeteredEnds) NextBoundaries(string id)
        {
            var result = new HashSet<string>();
            var unmetered = new HashSet<string>();
            var seen = new HashSet<string>();
            var queue = new Queue<string>(outgoing[id].Select(e => e.To));
            while (queue.TryDequeue(out var next))
            {
                if (!seen.Add(next)) continue;
                if (nodes[next].CanLinkMeter) result.Add(next);
                else if (outgoing[next].Count == 0) unmetered.Add(next);
                else foreach (var edge in outgoing[next]) queue.Enqueue(edge.To);
            }
            return (result, unmetered);
        }
        var previousById = map.Nodes.ToDictionary(n => n.Id, n => PreviousBoundaries(n.Id));
        var nextById = map.Nodes.Where(n => n.CanLinkMeter).ToDictionary(n => n.Id, n => NextBoundaries(n.Id));

        // A route is possible connectivity, not proof that a valve was open.
        // Follow each root's actual water type through source-restricted circuits.
        var sourceRoutes = map.Nodes.ToDictionary(n => n.Id, _ => new List<WaterSourceRoute>());
        foreach (var root in map.Nodes.Where(n => incoming[n.Id].Count == 0 || n.IsExternalSupply == true))
        {
            var origin = outgoing[root.Id].Count > 0 && outgoing[root.Id].All(e => e.Kind == "rain") ? "rain" : WaterAccounting.Origin(root);
            var paths = new Dictionary<string, (List<string> Path, bool Conditional)> { [root.Id] = ([root.Id], false) };
            var queue = new Queue<string>();
            queue.Enqueue(root.Id);
            while (queue.TryDequeue(out var id))
            {
                var path = paths[id];
                sourceRoutes[id].Add(new() { SourceId = root.Id, SourceLabel = root.Label, Origin = origin,
                    Path = string.Join(" → ", path.Path.Select(p => nodes[p].Label)), Conditional = path.Conditional });
                // Prefer a normal path if this root has both normal and backup routes.
                foreach (var edge in outgoing[id].OrderBy(e => e.Kind == "backup"))
                {
                    if ((edge.Kind == "rain" && origin != "rain") || (!string.IsNullOrEmpty(edge.SourceFilter) && edge.SourceFilter != origin) || paths.ContainsKey(edge.To)) continue;
                    paths[edge.To] = ([.. path.Path, edge.To], path.Conditional || edge.Kind == "backup");
                    queue.Enqueue(edge.To);
                }
            }
        }

        var points = new List<WaterFlowPoint>();
        var transfers = new List<WaterStationTransfer>();
        var ranks = new Dictionary<string, int>();
        int Rank(string id) => ranks.TryGetValue(id, out var rank) ? rank : ranks[id] = incoming[id].Count == 0 ? 0 : incoming[id].Max(e => Rank(e.From)) + 1;
        foreach (var node in map.Nodes.Where(n => n.CanLinkMeter))
        {
            var next = nextById[node.Id];
            var row = new WaterFlowPoint
            {
                Id = node.Id, Label = node.Label, Station = node.Station, Kind = node.Kind, ReportingRole=WaterAccounting.Role(node), Depth = Rank(node.Id),
                MeterId = valid.TryGetValue(node.Id, out var meterId) ? meterId : null,
                MeterName = valid.ContainsKey(node.Id) ? active[valid[node.Id]].MeterName : "Meter not linked",
                MeasuresFinalUse = node.MeasuresFinalUse, Flow = Scope([node.Id]), Sources = sourceRoutes[node.Id],
                NextPoints = next.Meters.Select(id => nodes[id].Label).ToList()
            };
            points.Add(row);
            foreach (var id in next.UnmeteredEnds) row.ComparisonIssues.Add($"Unmetered branch: {nodes[id].Label}.");
            foreach (var id in next.Meters)
            {
                var exclusive = previousById[id].SetEquals([node.Id]);
                if (!exclusive) row.ComparisonIssues.Add($"{nodes[id].Label} has another possible inlet. Its combined counter cannot be wholly assigned to {node.Label}.");
                if (nodes[id].Station != node.Station) transfers.Add(new()
                {
                    FromId = node.Id, ToId = id, From = node.Label, To = nodes[id].Label,
                    FromStation = node.Station, ToStation = nodes[id].Station, ExclusiveRoute = exclusive,
                    Received = WaterAccounting.CounterBoundary(nodes[id])=="inlet" ? Scope([id]) : Scope([]),
                    DedicatedDeparture = WaterAccounting.CounterBoundary(node)=="outlet" && next.Meters.Count==1 && next.UnmeteredEnds.Count==0,
                    Sent = WaterAccounting.CounterBoundary(node)=="outlet" && next.Meters.Count==1 && next.UnmeteredEnds.Count==0 ? Scope([node.Id]) : Scope([]),
                    MeasurementNote = WaterAccounting.CounterBoundary(nodes[id])=="outlet" ? "The next tank's counter records its own outflow, not arrival at that station. A shared Main outlet cannot isolate the Top transfer while other branches also use water." : "Receipt is recorded at the next station's incoming boundary."
                });
            }
            if (next.Meters.Count == 0) continue;
            var comparison = Scope(next.Meters.Append(node.Id));
            row.ComparisonIssues.AddRange(comparison.Missing);
            if (row.ComparisonIssues.Count > 0) continue;
            var dates = comparison.CompleteRows.Select(d => d.ReadingDate).ToHashSet();
            row.ComparedDays = dates.Count;
            if (dates.Count == 0) { row.ComparisonIssues.Add("Capture comparable readings for the parent and its next meters over matching dates."); continue; }
            row.ComparedParent = SumOnDays(valid[node.Id], dates);
            row.ComparedBranches = next.Meters.Sum(id => SumOnDays(valid[id], dates));
        }

        var inlet = map.Nodes.Where(n => WaterAccounting.Role(n) == "borehole-inlet").ToList();
        var outlet = map.Nodes.Where(n => WaterAccounting.Role(n) == "borehole-outlet").ToList();
        var pair = new BoreholeFlowPair { Incoming = Scope(inlet.Select(n => n.Id)), Outgoing = Scope(outlet.Select(n => n.Id)) };
        if (inlet.Count != 1 || outlet.Count != 1) pair.Issues.Add("Choose one Borehole Incoming and one Borehole Out reporting boundary in the network editor.");
        else if (!nextById[inlet[0].Id].Meters.SetEquals([outlet[0].Id]) || nextById[inlet[0].Id].UnmeteredEnds.Count > 0 || !previousById[outlet[0].Id].SetEquals([inlet[0].Id]))
            pair.Issues.Add("The borehole pair is not a simple inlet-to-outlet boundary on the map. Check additional meters and supply paths.");
        else
        {
            var matched = Scope([inlet[0].Id, outlet[0].Id]);
            pair.Issues.AddRange(matched.Missing);
            var dates = matched.CompleteRows.Select(d => d.ReadingDate).ToHashSet();
            pair.MatchedDays = dates.Count;
            if (pair.Issues.Count == 0 && dates.Count > 0)
            {
                pair.MatchedIncoming = SumOnDays(valid[inlet[0].Id], dates);
                pair.MatchedOutgoing = SumOnDays(valid[outlet[0].Id], dates);
            }
            else if (pair.Issues.Count == 0) pair.Issues.Add("The inlet and outlet readings do not yet cover matching dates.");
        }
        return new()
        {
            Start = start, End = end, Warnings = warnings, Borehole = pair, Transfers = transfers,
            MunicipalReceipt = Scope(map.Nodes.Where(n => WaterAccounting.Role(n) == "municipal-inlet").Select(n => n.Id)),
            Stations = new[] { "Lower", "Top" }.Select(station => new WaterStationFlow
            {
                Station = station, Points = points.Where(p => p.Station == station).OrderBy(p => p.Depth).ThenBy(p => WaterAccounting.Role(nodes[p.Id]) == "main" ? 0 : 1).ThenBy(p => p.Label).ToList()
            }).ToList()
        };
    }
}
