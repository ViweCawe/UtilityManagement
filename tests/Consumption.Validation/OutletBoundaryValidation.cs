using DataLibrary.Models;
using UtilityManagerProjects.Models;
using System.Text;
using System.Text.Json;

public static class OutletBoundaryValidation
{
    static void Equal<T>(T expected,T actual,string message){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"{message}: expected {expected}, got {actual}");}
    public static void Run()
    {
        var day=new DateTime(2025,1,1);
        var names=new[]{"main","dock","reservoir","dosing","header","outlet-1","outlet-2","outlet-3","outlet-4","outlet-5"};
        var amounts=new[]{30000,20000,15000,20000,20000,400,400,400,400,400};
        var map=new WaterNetwork{SourceBoundaryVersion=1,Nodes=names.Select((id,i)=>new WaterNetworkNode{Id=id,Label=id,Station=i==0?"Lower":"Top",Kind=i==0?"source":i<5?"storage":"usage",CounterBoundary=i>0&&i<5?"inlet":"outlet",ReportingRole=i==0?"main":"",SupplyOrigin="municipal",IsExternalSupply=false,MeasuresFinalUse=i>=5,MeterId=i+1}).ToList()};
        map.Nodes.Insert(0,new(){Id="inlet",Label="Municipal inlet",Station="Lower",Kind="source",ReportingRole="municipal-inlet",SupplyOrigin="municipal",IsExternalSupply=true});
        map.Connections.Add(new(){From="inlet",To="main"});
        foreach(var pair in new[]{("main","dock"),("dock","reservoir"),("reservoir","dosing"),("dosing","header")})map.Connections.Add(new(){From=pair.Item1,To=pair.Item2});
        for(var i=1;i<=5;i++)map.Connections.Add(new(){From="header",To="outlet-"+i});
        var boundaries=new Dictionary<string,(int[] In,int[] Out)>{["main"]=([20],[1]),["dock"]=([2],[3]),["reservoir"]=([3],[4]),["dosing"]=([4],[5]),["header"]=([5],[6,7,8,9,10])};
        foreach(var node in map.Nodes.Where(n=>boundaries.ContainsKey(n.Id)))node.Storage=new(){OpeningDate=day,OpeningVolumeLitres=node.Id=="reservoir"?20000:0,InletMeterIds=boundaries[node.Id].In.ToList(),OutletMeterIds=boundaries[node.Id].Out.ToList(),AllFlowsConfirmed=true};
        map.Validate();
        var links=names.Select((id,i)=>(id,i)).ToDictionary(p=>p.id,p=>(int?)(p.i+1));links["inlet"]=null;
        var meters=Enumerable.Range(1,10).Append(20).Select(i=>new Meter{Id=i,MeterName="Meter "+i,MeterType=MeterType.Water,IsActive=true}).ToList();
        var history=amounts.SelectMany((delta,i)=>new[]{new MeterReading{Id=i*2+1,MeterId=i+1,ReadingDate=day,CurrentReading=10000*(i+1),MeterType=MeterType.Water},new MeterReading{Id=i*2+2,MeterId=i+1,ReadingDate=day.AddDays(1),CurrentReading=10000*(i+1)+delta,MeterType=MeterType.Water}}).ToList();
        var report=WaterStorageAccounting.Calculate(map,links,meters,history,day.AddDays(1),day.AddDays(1));
        var source=report.Balances.Single(b=>b.NodeId=="main");
        Equal(30000m,source.PeriodOutflow,"Main counter delta is recorded source outlet flow");
        Equal(null,source.PeriodInflow,"unrecorded municipal inlet is unknown, never invented as 100000 L");
        Equal(null,source.CalculatedStock,"source closing stock cannot be derived from an unrecorded inlet");
        Equal(5000m,report.Balances.Single(b=>b.NodeId=="dock").CalculatedStock,"Dock 20000 in minus 15000 out");
        Equal(15000m,report.Balances.Single(b=>b.NodeId=="reservoir").CalculatedStock,"Reservoir 20000 opening plus 15000 received minus 20000 leaving");
        Equal(0m,report.Balances.Single(b=>b.NodeId=="dosing").CalculatedStock,"Dosing transfers the same 20000 onward");
        Equal(18000m,report.Balances.Single(b=>b.NodeId=="header").CalculatedStock,"Header delivers 2000 across ALL five outlets, leaving 18000");
        Equal(2000m,report.FinalUse.Total,"only the five end-use counters contribute consumption");
        Equal(true,report.FinalUse.IsComplete,"known end-use consumption stays available despite unrecorded municipal inlet");
        Equal(40000m,report.Balances.Where(b=>b.NodeId!="main").Sum(b=>b.CalculatedStock)+report.FinalUse.Total,"Top opening 20000 plus transfer 20000 reconciles to stock 38000 plus use 2000");
        var selection=WaterConsumptionBoundaries.Resolve(map,links,meters);
        Equal(true,selection.MeterIds.SetEquals(new[]{6,7,8,9,10}),"default EndUse registry values never cause source, transfer or tank counters to enter consumption");
        links["inlet"]=20;history.Add(new(){Id=100,MeterId=20,ReadingDate=day,CurrentReading=500000,MeterType=MeterType.Water});history.Add(new(){Id=101,MeterId=20,ReadingDate=day.AddDays(1),CurrentReading=600000,MeterType=MeterType.Water});
        Equal(70000m,WaterStorageAccounting.Calculate(map,links,meters,history,day.AddDays(1),day.AddDays(1)).Balances.Single(b=>b.NodeId=="main").CalculatedStock,"70000 source stock only appears when actual inlet captures record 100000 L");
        // A mid-interval report needs opening stock at its own start, not the old anchor.
        foreach(var capture in history.Where(h=>h.ReadingDate>day))capture.ReadingDate=day.AddDays(5);
        var clipped=WaterStorageAccounting.Calculate(map,links,meters,history,day.AddDays(3),day.AddDays(5));
        foreach(var row in clipped.Balances)Equal(row.CalculatedStock,row.PeriodOpeningStock+row.PeriodInflow-row.PeriodOutflow,"each displayed tank equation reconciles for a clipped reporting period");
        Equal(2000m,clipped.Balances.Single(b=>b.NodeId=="dock").PeriodOpeningStock,"Dock opening is rolled forward from the measured anchor to the selected start");
        var legacy=new WaterNetwork{Nodes=[new(){Id="main-tank",Label="Main Meter — Source",Station="Lower",Kind="source",ReportingRole="main",SupplyOrigin="municipal",IsExternalSupply=true}],Connections=[]};
        WaterNetwork Reload(WaterNetwork value){using var stream=new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));return WaterNetwork.Load(stream);}
        var migrated=Reload(legacy);var twice=Reload(migrated);
        Equal(2,twice.Nodes.Count,"legacy map gains one physical unrecorded inlet, idempotently");
        Equal(false,twice.Nodes.Single(n=>n.Id=="main-tank").IsExternalSupply,"Main outlet is not an external receipt boundary");
        Equal(1,twice.Nodes.Count(n=>WaterAccounting.Role(n)=="municipal-inlet"),"one unlinked inlet reporting role");
        var main=map.Nodes.Single(n=>n.Id=="main");main.Storage!.InletMeterIds=[1];
        Equal(null,WaterStorageAccounting.Calculate(map,links,meters,history,day.AddDays(1),day.AddDays(5)).Balances.Single(b=>b.NodeId=="main").CalculatedStock,"legacy settings cannot mistake Main's outlet as incoming supply");
        var fixture=new Fixture(day);fixture.graph.Nodes.Single(n=>n.Id=="toilet").MeasuresFinalUse=true;
        var service=fixture.Service();service.LoadAsync().GetAwaiter().GetResult();var period=service.ForPeriod(day.AddDays(1),day.AddDays(7));
        var consumers=service.ConsumptionTable(period);
        Equal(true,consumers.Rows.Select(r=>r.MeterId).SequenceEqual(new[]{6}),"consumption table excludes Main, Borehole In/Out, Garage and other transfers even with legacy EndUse registry defaults");
        var boreBalance=service.StorageForPeriod(day.AddDays(1),day.AddDays(7)).Balances.Single(b=>b.NodeId=="incoming");
        Equal(140m,boreBalance.PeriodInflow,"simple borehole pair shows actual inlet movement in tank table without a fabricated opening stock");
        Equal(56m,boreBalance.PeriodOutflow,"simple borehole pair shows actual outlet movement");
        Equal(null,boreBalance.CalculatedStock,"paired movements alone do not establish remaining borehole stock");
        Equal(14m,consumers.Scope.Total,"only the confirmed end-use meter contributes to the consumption total");
        Equal(0,service.ConsumptionTable(service.ForPeriod(day.AddDays(1),day.AddDays(7),1),1).Rows.Count,"filtering by a source meter does not reclassify it as consumption");
        Equal(0,period.MunicipalReceipt.CoveredDays,"unrecorded inlet never inherits Main outlet reading coverage");
        Equal("Source outlet / distribution",service.ReadingPurpose(1),"raw source reading is labelled distribution, not consumption");
        fixture.graph.SourceBoundaryVersion=1;fixture.graph.Nodes.Single(n=>n.Id=="main").IsExternalSupply=false;
        fixture.graph.Nodes.Add(new(){Id="receipt",Label="Municipal inlet",Station="Lower",Kind="source",ReportingRole="municipal-inlet",SupplyOrigin="municipal",IsExternalSupply=true});fixture.graph.Connections.Add(new(){From="receipt",To="main"});fixture.Links["receipt"]=20;
        fixture.Meters.Add(new(){Id=20,MeterName="Municipal inlet",MeterType=MeterType.Water,DepartmentName="Supply",IsActive=true});
        fixture.History.Add(new(){Id=1000,MeterId=20,ReadingDate=day,CurrentReading=100000,MeterType=MeterType.Water});fixture.History.Add(new(){Id=1001,MeterId=20,ReadingDate=day.AddDays(7),CurrentReading=103000,MeterType=MeterType.Water});
        var recorded=fixture.Service();recorded.LoadAsync().GetAwaiter().GetResult();
        Equal(700m,recorded.ForPeriod(day.AddDays(1),day.AddDays(7),department:"Supply").Water.Total,"recorded inlet does not replace Main outlet in department distribution totals");
        Equal(3000m,decimal.Round(recorded.ForPeriod(day.AddDays(1),day.AddDays(7)).MunicipalReceipt.Total,6),"actual inlet counter deltas remain separately reportable when recorded later");
        fixture.graph.Nodes.Add(new(){Id="missing-final",Label="Unlinked final meter",Station="Lower",Kind="usage",MeasuresFinalUse=true});
        var partiallyMapped=fixture.Service();partiallyMapped.LoadAsync().GetAwaiter().GetResult();
        Equal(false,partiallyMapped.ConsumptionTable(partiallyMapped.ForPeriod(day.AddDays(1),day.AddDays(7))).Scope.IsComplete,"unlinked confirmed end use prevents an invented complete consumption total");
        Equal(true,partiallyMapped.ConsumptionTable(partiallyMapped.ForPeriod(day.AddDays(1),day.AddDays(7),6),6).Scope.IsComplete,"an unrelated unlinked end use does not hide a selected meter's own consumption");
        Console.WriteLine("PASS: separate unrecorded inlet, recorded Main outlet, corrected 2000 L Header total, consumption-only selection, Top conservation, period-aligned tank equations and idempotent map migration.");
    }
}
