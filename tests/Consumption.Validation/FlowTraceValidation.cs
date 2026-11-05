using DataLibrary.Models;
using UtilityManagerProjects.Models;

public static class FlowTraceValidation
{
    static readonly DateTime Day = new(2025, 1, 1);
    static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{message}: expected {expected}, got {actual}");
    }
    static MeterReading Reading(int id, DateTime date, int counter) => new() { Id = id * 100 + date.Day, MeterId = id, ReadingDate = date, CurrentReading = counter, MeterType = MeterType.Water };
    static Meter Meter(int id) => new() { Id = id, MeterName = $"Actual meter {id}", MeterType = MeterType.Water, IsActive = true };
    public static void Run()
    {
        var names = new[] { "main", "son", "daughter", "store", "assistant", "friend-1", "friend-2", "friend-3", "friend-4", "friend-5", "house-1", "house-2", "house-3", "house-4", "house-5" };
        var amounts = new[] { 2000, 500, 600, 350, 200, 100, 100, 100, 100, 100, 10, 10, 10, 10, 10 };
        var map = new WaterNetwork
        {
            Nodes = names.Select((name, i) => new WaterNetworkNode
            {
                Id = name, Label = name, Station = i is 3 or 4 || i >= 10 ? "Top" : "Lower",
                Kind = i == 0 ? "source" : i == 3 ? "storage" : i < 5 ? "distribution" : "usage",
                ReportingRole = i == 0 ? "main" : "", IsExternalSupply = i == 0, SupplyOrigin = "municipal", MeasuresFinalUse = i >= 5
            }).ToList(),
            Connections = [new() { From = "main", To = "son" }, new() { From = "main", To = "daughter" }, new() { From = "daughter", To = "handoff" }, new() { From = "handoff", To = "store" }, new() { From = "store", To = "assistant" }]
        };
        map.Nodes.Add(new() { Id = "handoff", Label = "Transfer to Top", Station = "Top", Kind = "transfer", CanLinkMeter = false });
        for (int i = 1; i <= 5; i++) { map.Connections.Add(new() { From = "son", To = $"friend-{i}" }); map.Connections.Add(new() { From = "assistant", To = $"house-{i}" }); }
        map.Validate();
        var links = names.Select((name, i) => (name, i)).ToDictionary(p => p.name, p => (int?)(p.i + 1));
        var meters = amounts.Select((_, i) => Meter(i + 1)).ToList();
        var readings = amounts.SelectMany((amount, i) => new[] { Reading(i + 1, Day, 10000), Reading(i + 1, Day.AddDays(1), 10000 + amount) }).ToList();
        WaterFlowTraceReport Report() => WaterFlowTracing.Calculate(map, links, meters, readings, Day.AddDays(1), Day.AddDays(1));
        var report = Report();
        Equal(8, report.Stations.Single(s => s.Station == "Lower").Points.Count, "Lower Station includes its own metered branches");
        Equal(7, report.Stations.Single(s => s.Station == "Top").Points.Count, "Top Station includes tanks and farms");
        var root = report.Points.Single(p => p.Id == "main");
        Equal(1100m, root.ComparedBranches, "Main compares only Son plus Daughter; grandchildren are excluded");
        Equal(900m, root.Difference, "Main difference after its immediate handovers");
        Equal(250m, report.Points.Single(p => p.Id == "daughter").Difference, "Daughter's 600 minus 350 transfer remains visible");
        Equal(150m, report.Points.Single(p => p.Id == "store").Difference, "Storage transfer 350 minus 200");
        Equal(150m, report.Points.Single(p => p.Id == "assistant").Difference, "Assistant's 200 minus five houses of 10");
        Equal(2000m, report.Points.Sum(p => p.Difference ?? 0) + report.Points.Where(p => p.MeasuresFinalUse).Sum(p => p.Flow.Total), "all differences plus measured end use reconcile to Main exactly once");
        Equal(350m, report.Transfers.Single().Received.Total, "Top entry uses receiving meter, skips unmetered handoff");
        Equal("daughter", report.Transfers.Single().FromId, "station receipt retains immediate parent boundary");
        Equal(true, report.Points.Single(p => p.Id == "house-1").Sources.Single().Path.Contains("main → daughter → Transfer to Top → store → assistant → house-1"), "every downstream house traces across stations to Main");
        // The same handovers can come from existing stock, with no new water through Main.
        var storageIds = new HashSet<string> { "main", "son", "daughter", "store", "assistant" };
        var storedGraph = new WaterNetwork
        {
            Nodes = map.Nodes.Select(n => new WaterNetworkNode
            {
                Id=n.Id, Label=n.Label, Station=n.Station, Kind=storageIds.Contains(n.Id)?n.Id=="main"?"source":"storage":n.Kind,
                CanLinkMeter=n.CanLinkMeter, ReportingRole=n.ReportingRole, SupplyOrigin=n.SupplyOrigin,
                IsExternalSupply=n.IsExternalSupply, MeasuresFinalUse=n.MeasuresFinalUse
            }).ToList(), Connections=map.Connections.ToList()
        };
        var stockBoundaries = new Dictionary<string,(int[] In,int[] Out)>
        {
            ["main"]=([1],[2,3]), ["son"]=([2],[6,7,8,9,10]), ["daughter"]=([3],[4]),
            ["store"]=([4],[5]), ["assistant"]=([5],[11,12,13,14,15])
        };
        foreach(var node in storedGraph.Nodes.Where(n=>storageIds.Contains(n.Id)))
            node.Storage=new(){OpeningDate=Day,OpeningVolumeLitres=node.Id=="main"?2000:0,InletMeterIds=stockBoundaries[node.Id].In.ToList(),OutletMeterIds=stockBoundaries[node.Id].Out.ToList(),AllFlowsConfirmed=true};
        storedGraph.Validate();
        var stockCaptures=readings.Select(r=>new MeterReading{Id=r.Id,MeterId=r.MeterId,ReadingDate=r.ReadingDate,CurrentReading=r.MeterId==1?10000:r.CurrentReading,MeterType=MeterType.Water}).ToList();
        var stored=WaterStorageAccounting.Calculate(storedGraph,links,meters,stockCaptures,Day.AddDays(1),Day.AddDays(1));
        Equal(900m,stored.Balances.Single(b=>b.NodeId=="main").CalculatedStock,"actual opening tank stock 2000 minus Son 500 and Daughter 600 leaves 900 without Main counter movement");
        Equal(250m,stored.Balances.Single(b=>b.NodeId=="daughter").CalculatedStock,"Daughter holds the 250 not passed into storage");
        Equal(2000m,stored.Balances.Sum(b=>b.CalculatedStock)+stored.FinalUse.Total,"opening stock is conserved across every handover and final use");
        Equal(-1100m,WaterFlowTracing.Calculate(storedGraph,links,meters,stockCaptures,Day.AddDays(1),Day.AddDays(1)).Points.Single(p=>p.Id=="main").Difference,"stock drawdown is a signed movement difference, not a false negative consumption total");
        links.Remove("son");
        Equal(null, Report().Points.Single(p => p.Id == "main").Difference, "missing parent link is not replaced by its grandchildren");
        links["son"] = 2;
        map.Connections.Add(new() { From = "daughter", To = "friend-1" });
        Equal(null, Report().Points.Single(p => p.Id == "son").Difference, "combined descendant cannot be wholly charged to one parent");
        map.Connections.RemoveAt(map.Connections.Count - 1);
        // Match the comparison dates rather than subtracting different-length totals.
        var staggered = readings.Select(r => new MeterReading { Id = r.Id, MeterId = r.MeterId, ReadingDate = r.ReadingDate == Day ? Day : Day.AddDays(r.MeterId == 2 ? 5 : 10), CurrentReading = r.CurrentReading, MeterType = MeterType.Water }).ToList();
        var aligned = WaterFlowTracing.Calculate(map, links, meters, staggered, Day.AddDays(1), Day.AddDays(10)).Points.Single(p => p.Id == "main");
        Equal(5, aligned.ComparedDays, "comparison uses common covered dates");
        Equal(1000m, aligned.ComparedParent, "parent clipped to the five common days");
        Equal(800m, aligned.ComparedBranches, "Son 500 plus Daughter 300 on common dates");
        Equal(2000m, aligned.Flow.Total, "individual meter still retains its full covered period");

        var bore = new WaterNetwork
        {
            Nodes = [new() { Id = "in", Label = "Incoming", Kind = "storage", Station = "Lower", ReportingRole = "borehole-inlet", SupplyOrigin = "borehole", IsExternalSupply = true }, new() { Id = "out", Label = "Out", Kind = "distribution", Station = "Lower", ReportingRole = "borehole-outlet" }],
            Connections = [new() { From = "in", To = "out" }]
        };
        bore.Validate();
        var boreLinks = new Dictionary<string, int?> { ["in"] = 1, ["out"] = 2 };
        var boreReadings = new List<MeterReading> { Reading(1, Day, 10000), Reading(1, Day.AddDays(7), 10700), Reading(2, Day, 25000), Reading(2, Day.AddDays(7), 25350) };
        WaterFlowTraceReport Bore() => WaterFlowTracing.Calculate(bore, boreLinks, [Meter(1), Meter(2)], boreReadings, Day.AddDays(1), Day.AddDays(7));
        Equal(700m, Bore().Borehole.Incoming.Total, "borehole inlet delta");
        Equal(350m, Bore().Borehole.Outgoing.Total, "borehole supplied usage is outlet delta, independent counter baseline");
        Equal(350m, Bore().Borehole.Difference, "paired borehole input minus output, without opening-stock requirement");
        Equal("in", Bore().Points.Single(p => p.Id == "out").Sources.Single().SourceId, "borehole water traces to its independent inlet, not Main");
        boreReadings[1].CurrentReading = 10100;
        Equal(-250m, Bore().Borehole.Difference, "drawing down previously stored water is a valid negative net difference");
        boreReadings.RemoveAt(1);
        Equal(null, Bore().Borehole.Difference, "missing inlet interval keeps the net unknown");
        Equal(350m, Bore().Borehole.Outgoing.Total, "known borehole output stays visible when input is missing");
        boreReadings = [Reading(1, Day, 10000), Reading(1, Day.AddDays(10), 10700), Reading(2, Day.AddDays(2), 25000), Reading(2, Day.AddDays(7), 25350)];
        Equal(5, Bore().Borehole.MatchedDays, "borehole pair aligns staggered intervals");
        Equal(0m, Bore().Borehole.Difference, "350 in minus 350 out on matching days");
        bore.Connections.Clear();
        Equal(null, Bore().Borehole.Difference, "disconnected counters do not invent a tank pair");
        CircuitRoutes();
        Console.WriteLine("PASS: both station flows, family handover reconciliation, cross-station trace, matching dates, unlinked parent and combined-inlet guards, paired borehole counters and independent origins.");
    }
    static void CircuitRoutes()
    {
        var graph = new WaterNetwork
        {
            Nodes = [new() { Id = "main", Label = "Main", Kind = "source", Station = "Lower", ReportingRole = "main", SupplyOrigin = "municipal" }, new() { Id = "bore", Label = "Borehole", Kind = "source", Station = "Lower", SupplyOrigin = "borehole" }, new() { Id = "workshop", Label = "Workshop", Kind = "distribution", Station = "Lower" }, new() { Id = "basins", Label = "Basins", Kind = "usage", Station = "Lower" }, new() { Id = "rain", Label = "Rain", Kind = "source", Station = "Top", CanLinkMeter = false }, new() { Id = "tank", Label = "Tank", Kind = "storage", Station = "Top" }],
            Connections = [new() { From = "main", To = "workshop", SourceFilter = "municipal" }, new() { From = "bore", To = "workshop", SourceFilter = "borehole" }, new() { From = "workshop", To = "basins", SourceFilter = "municipal" }, new() { From = "main", To = "tank" }, new() { From = "rain", To = "tank", Kind = "rain" }]
        };
        graph.Validate();
        var trace = WaterFlowTracing.Calculate(graph, new Dictionary<string, int?>(), [], [], Day, Day);
        Equal(2, trace.Points.Single(p => p.Id == "workshop").Sources.Count, "shared location retains both possible sources");
        Equal("main", trace.Points.Single(p => p.Id == "basins").Sources.Single().SourceId, "borehole flushing cannot acquire municipal-only basin circuit");
        Equal(true, trace.Points.Single(p => p.Id == "tank").Sources.Any(s => s.Origin == "rain"), "unmetered rain input keeps its independent source path");
    }
}
