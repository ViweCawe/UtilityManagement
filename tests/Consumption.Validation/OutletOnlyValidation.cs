using DataLibrary.Models;
using UtilityManagerProjects.Models;

public static class OutletOnlyValidation
{
 static void Equal<T>(T expected,T actual,string message){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"{message}: expected {expected}, got {actual}");}
 public static void Run()
 {
  var day=new DateTime(2025,1,1);
  var names=new[]{"main","dock","reservoir","dosing","header","use1","use2","use3","use4","use5","loading"};
  var amounts=new[]{30000,15000,20000,20000,2000,400,400,400,400,400,20000};
  var map=new WaterNetwork{SourceBoundaryVersion=1,Nodes=names.Select((id,i)=>new WaterNetworkNode{Id=id,Label=id,Station=i==0||i==10?"Lower":"Top",Kind=i==0?"source":i<5?"storage":i==10?"transfer":"usage",CounterBoundary="outlet",ReportingRole=i==0?"main":"",SupplyOrigin="municipal",MeasuresFinalUse=i>=5&&i<=9}).ToList()};
  map.Nodes.Add(new(){Id="inlet",Label="Unrecorded municipal inlet",Station="Lower",Kind="source",ReportingRole="municipal-inlet",SupplyOrigin="municipal",IsExternalSupply=true});
  map.Nodes.Add(new(){Id="lower-use",Label="Other lower branches",Station="Lower",Kind="usage",CanLinkMeter=false});
  foreach(var (from,to) in new[]{("inlet","main"),("main","loading"),("main","lower-use"),("loading","dock"),("dock","reservoir"),("reservoir","dosing"),("dosing","header")})map.Connections.Add(new(){From=from,To=to});
  for(var i=1;i<=5;i++)map.Connections.Add(new(){From="header",To="use"+i});
  var inbound=new[]{20,11,2,3,4};
  for(var i=0;i<5;i++)map.Nodes[i].Storage=new(){OpeningDate=day,OpeningVolumeLitres=i==2?20000:0,InletMeterIds=[inbound[i]],OutletMeterIds=[i+1],AllFlowsConfirmed=true,IncomingTransfersConfirmed=true};
  map.Validate();
  var links=names.Select((id,i)=>(id,i)).ToDictionary(p=>p.id,p=>(int?)(p.i+1));links["inlet"]=null;
  var meters=Enumerable.Range(1,11).Append(20).Select(i=>new Meter{Id=i,MeterName="Meter "+i,MeterType=MeterType.Water,IsActive=true}).ToList();
  var history=amounts.SelectMany((delta,i)=>new[]{new MeterReading{Id=i*2+1,MeterId=i+1,ReadingDate=day,CurrentReading=100000,MeterType=MeterType.Water},new MeterReading{Id=i*2+2,MeterId=i+1,ReadingDate=day.AddDays(1),CurrentReading=100000+delta,MeterType=MeterType.Water}}).ToList();
  WaterStorageReport Report()=>WaterStorageAccounting.Calculate(map,links,meters,history,day.AddDays(1),day.AddDays(1));
  var report=Report();
  Equal(null,report.Balances.Single(b=>b.NodeId=="main").CalculatedStock,"unrecorded source inlet prevents stock calculation");
  foreach(var (id,stock) in new[]{("dock",5000m),("reservoir",15000m),("dosing",0m),("header",18000m)})Equal(stock,report.Balances.Single(b=>b.NodeId==id).CalculatedStock,"outlet-only balance "+id);
  Equal(15000m,report.Balances.Single(b=>b.NodeId=="dock").PeriodOutflow,"Dock counter records Dock to Reservoir, not receipt");
  Equal(2000m,report.FinalUse.Total,"transfers never inflate final consumption");
  var trace=WaterFlowTracing.Calculate(map,links,meters,history,day.AddDays(1),day.AddDays(1));
  var crossing=trace.Transfers.Single(t=>t.ToId=="dock");
  Equal(20000m,crossing.Sent.Total,"dedicated loading outlet measures departure");
  Equal(0,crossing.Received.MeterIds.Count,"Dock outlet cannot measure transport arrival");
  map.Nodes[1].Storage!.IncomingTransfersConfirmed=false;
  Equal(null,Report().Balances.Single(b=>b.NodeId=="dock").CalculatedStock,"unconfirmed arrival does not invent stock");
  Equal(20000m,Report().Balances.Single(b=>b.NodeId=="dock").UpstreamDispatched,"departure stays visible without arrival confirmation");
  map.Nodes[1].Storage!.IncomingTransfersConfirmed=true;map.Nodes[1].Storage!.InletMeterIds=[1];
  Equal(null,Report().Balances.Single(b=>b.NodeId=="dock").PeriodInflow,"whole shared Main outlet is not assigned to Top");
  map.Nodes[1].Storage!.InletMeterIds=[2];
  Equal(null,Report().Balances.Single(b=>b.NodeId=="dock").PeriodInflow,"own Dock outlet is never its inlet");
  var intraday=new List<MeterReading>{new(){Id=100,MeterId=11,ReadingDate=day.AddHours(10),CurrentReading=200},new(){Id=101,MeterId=11,ReadingDate=day.AddHours(11),CurrentReading=300},new(){Id=102,MeterId=11,ReadingDate=day.AddHours(12),CurrentReading=150}};
  var movements=WaterOutletMovements.Calculate(map,links,meters,intraday,day,day);
  Equal(100m,movements.Rows.Single(r=>r.ClosingId==101).Volume,"200 to 300 is 100 L measured departure, even on the same day");
  Equal(null,movements.Rows.Single(r=>r.ClosingId==102).Volume,"reset is not a negative transfer");
  Equal(0,IntervalConsumption.Allocate(intraday,day,day).Count,"intraday change does not pretend to cover a full daily average");
  Equal(false,WaterOutletMovements.Calculate(map,links,meters,history,day.AddDays(1),day.AddDays(1)).Rows.Any(r=>r.MeterId>=6&&r.MeterId<=10),"final-use counters stay in consumption table");
  Console.WriteLine("PASS: outlet-only tank chain, confirmed receipts, dedicated dispatch boundary, shared-Main guard, consumption separation and same-day 200 to 300 = 100 L evidence.");
 }
}
