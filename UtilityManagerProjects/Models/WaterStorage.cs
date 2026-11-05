using DataLibrary.Models;
namespace UtilityManagerProjects.Models;

public sealed class WaterStorageBalance
{
 public string NodeId { get; init; } = "";
 public string Name { get; init; } = "";
 public decimal? PeriodOpeningStock { get; set; }
 public decimal? OpeningVolume { get; set; }
 public DateTime? OpeningDate { get; set; }
 public decimal? PeriodInflow { get; set; }
 public decimal? PeriodOutflow { get; set; }
 public decimal? PeriodChange { get; set; }
 public decimal? CalculatedStock { get; set; }
 public string Status { get; set; } = "";
}
public sealed class WaterTransferRow
{
 public int MeterId { get; init; }
 public string MeterName { get; init; } = "";
 public string From { get; init; } = "";
 public string To { get; init; } = "";
 public decimal? Volume { get; init; }
}
public sealed class WaterPointFlow
{
 public string Point { get; init; } = "";
 public string Suppliers { get; init; } = "";
 public string Meter { get; init; } = "";
 public decimal? Flow { get; init; }
 public int CoveredDays { get; init; }
}
public sealed class WaterStorageReport
{
 public List<WaterPointFlow> PointFlows { get; init; } = [];
 public List<WaterStorageBalance> Balances { get; init; } = [];
 public List<WaterTransferRow> Transfers { get; init; } = [];
 public ConsumptionScope FinalUse { get; init; } = new();
}
public static class WaterStorageAccounting
{
 public static WaterStorageReport Calculate(WaterNetwork map,IReadOnlyDictionary<string,int?> links,List<Meter> meters,List<MeterReading> history,DateTime start,DateTime end,BoreholeFlowPair? borehole=null)
 {
  start=start.Date;end=end.Date;
  var active=meters.Where(m=>m.IsActive&&m.MeterType==MeterType.Water).ToDictionary(m=>m.Id);
  var allocations=IntervalConsumption.Allocate(history.Where(h=>active.ContainsKey(h.MeterId)),start,end);
  ConsumptionScope Scope(IEnumerable<int> ids,DateTime from,DateTime to,List<DailyUsage>? rows=null) => new(){Start=from,End=to,MeterIds=ids.ToHashSet(),Allocated=rows??allocations};
  var selection=WaterConsumptionBoundaries.Resolve(map,links,meters);
  var finalScope=new ConsumptionScope{Start=start,End=end,MeterIds=selection.MeterIds,Missing=selection.Issues,Allocated=allocations,Label="Recorded final consumption"};
  var balances=new List<WaterStorageBalance>();
  var storageNodes=map.Nodes.Where(n=>n.Kind=="storage"||n.Storage is not null||WaterAccounting.Role(n)=="main").ToList();
  foreach(var node in storageNodes) {
   var row=new WaterStorageBalance{NodeId=node.Id,Name=WaterAccounting.Role(node)=="main"?"Main/source tank":node.Label};balances.Add(row);var settings=node.Storage;
   var main=WaterAccounting.Role(node)=="main";
   var ownId=links.GetValueOrDefault(node.Id);
   if(main && ownId.HasValue && active.ContainsKey(ownId.Value)) {
    var released=Scope(new[]{ownId.Value},start,end);if(released.IsComplete)row.PeriodOutflow=released.Total;
   }
   if(settings is null && WaterAccounting.Role(node)=="borehole-inlet") {
    var pair=borehole??WaterFlowTracing.Calculate(map,links,meters,history,start,end).Borehole;
    if(pair.Issues.Count==0) {
     if(pair.Incoming.IsComplete)row.PeriodInflow=pair.Incoming.Total;
     if(pair.Outgoing.IsComplete)row.PeriodOutflow=pair.Outgoing.Total;
    }
    row.Status="Paired borehole inlet/outlet movements are shown where fully covered. A measured opening stock is still needed for remaining volume.";continue;
   }
   if(settings is null){row.Status=main?"Main measures the source outlet. Municipal inflow and opening stock are not recorded, so remaining source stock is unknown.":"Storage boundaries and opening volume are not configured.";continue;}
   row.OpeningDate=settings.OpeningDate?.Date;row.OpeningVolume=settings.OpeningVolumeLitres;
   if(row.OpeningDate==start.AddDays(-1))row.PeriodOpeningStock=row.OpeningVolume;
   if(row.OpeningDate==end && row.OpeningVolume.HasValue) row.CalculatedStock=row.OpeningVolume;
   if(main && ownId.HasValue && settings.InletMeterIds.Contains(ownId.Value)){row.Status="Review tank boundaries: Main is the outlet, not the municipal inlet. Water received and closing stock remain unknown.";continue;}
   bool Linked(int id)=>active.ContainsKey(id)&&links.Any(p=>p.Value==id&&map.Nodes.Any(n=>n.Id==p.Key));
   var inScope=Scope(settings.InletMeterIds,start,end);var outScope=Scope(settings.OutletMeterIds,start,end);
   if(settings.InletMeterIds.Count>0&&settings.InletMeterIds.All(Linked)&&inScope.IsComplete)row.PeriodInflow=inScope.Total;
   if(settings.OutletMeterIds.Count>0&&settings.OutletMeterIds.All(Linked)&&outScope.IsComplete)row.PeriodOutflow=outScope.Total;
   var all=settings.InletMeterIds.Concat(settings.OutletMeterIds).ToHashSet();
   if(settings.InletMeterIds.Count==0||settings.OutletMeterIds.Count==0){row.Status="Identify measured inlet and outlet counters. An unrecorded inlet or outlet is unknown, not zero.";continue;}
   if(all.Any(id=>!Linked(id))){row.Status="A boundary meter is inactive or not linked on the map.";continue;}
   var parentOrigins=WaterAccounting.Origins(map,node.Id);
   if(settings.InletMeterIds.Any(id=>links.Where(p=>p.Value==id).SelectMany(p=>WaterAccounting.Origins(map,p.Key)).Any(origin=>!parentOrigins.Contains(origin)))){row.PeriodInflow=null;row.Status="An inlet counter measures a different or additional source. Check the physical inlet boundary before calculating stock.";continue;}
   if(settings.OutletMeterIds.Any(id=>links.Where(p=>p.Value==id).SelectMany(p=>WaterAccounting.Origins(map,p.Key)).Any(origin=>!parentOrigins.Contains(origin)))){row.PeriodOutflow=null;row.Status="An outlet counter also includes another source. Its combined reading cannot be debited to this single-source tank.";continue;}
   var period=Scope(all,start,end);
   if(period.IsComplete){row.PeriodInflow=Scope(settings.InletMeterIds,start,end).Total;row.PeriodOutflow=Scope(settings.OutletMeterIds,start,end).Total;if(settings.AllFlowsConfirmed)row.PeriodChange=row.PeriodInflow-row.PeriodOutflow;}
   if(!settings.AllFlowsConfirmed){row.Status="Confirm every inflow/outflow is measured before interpreting the difference as a storage change.";continue;}
   if(row.OpeningDate is null||row.OpeningVolume is null){row.Status="Opening stock unknown. Meter differences can show movement, not litres remaining.";continue;}
   var opening=row.OpeningDate.Value;
   if(opening>end){row.Status="The opening measurement is after this report's end date.";continue;}
   if(opening==end){row.CalculatedStock=row.OpeningVolume;row.Status="Measured opening stock at end of this date.";continue;}
   if((end-opening).Days>3660){row.Status="Record a more recent physical opening stock measurement.";continue;}
   var since=IntervalConsumption.Allocate(history.Where(h=>active.ContainsKey(h.MeterId)),opening.AddDays(1),end);
   var window=Scope(all,opening.AddDays(1),end,since);
   if(!window.IsComplete){row.Status="Readings do not cover all dates from the opening measurement to the report end. Stored volume is unknown.";continue;}
   var stock=row.OpeningVolume.Value+Scope(settings.InletMeterIds,opening.AddDays(1),end,since).Total-Scope(settings.OutletMeterIds,opening.AddDays(1),end,since).Total;
   if(stock<0){row.Status="Inconsistent boundary: calculated stock would be negative. Check opening volume, meter direction and missing sources.";continue;}
   if(opening<=start.AddDays(-1)) {
    if(opening==start.AddDays(-1))row.PeriodOpeningStock=row.OpeningVolume;
    else {
     var prior=since.Where(d=>d.ReadingDate<start).ToList();var priorScope=Scope(all,opening.AddDays(1),start.AddDays(-1),prior);
     if(priorScope.IsComplete) {
      var openingStock=row.OpeningVolume.Value+Scope(settings.InletMeterIds,opening.AddDays(1),start.AddDays(-1),prior).Total-Scope(settings.OutletMeterIds,opening.AddDays(1),start.AddDays(-1),prior).Total;
      if(openingStock>=0)row.PeriodOpeningStock=openingStock;
     }
    }
   }
   row.CalculatedStock=stock;row.Status="Calculated from measured opening stock and recorded flow intervals; estimated between captures, not a tank-level reading.";
  }
  var transfers=new List<WaterTransferRow>();
  foreach(var from in storageNodes.Where(n=>n.Storage is not null)) foreach(var id in from.Storage!.OutletMeterIds) {
   var to=storageNodes.SingleOrDefault(n=>n.Storage?.InletMeterIds.Contains(id)==true);
   if(to is null)continue;
   var scope=Scope(new[]{id},start,end);
   transfers.Add(new(){MeterId=id,MeterName=active.GetValueOrDefault(id)?.MeterName??$"Meter #{id}",From=from.Label,To=to.Label,Volume=scope.IsComplete?scope.Total:null});
  }
  var points=map.Nodes.Where(n=>n.CanLinkMeter).Select(n=> {
   var id=links.GetValueOrDefault(n.Id);var rows=id.HasValue?allocations.Where(d=>d.MeterId==id.Value).ToList():[];
   var parents=map.Connections.Where(e=>e.To==n.Id).Select(e=>map.Nodes.Single(p=>p.Id==e.From).Label).ToList();
   return new WaterPointFlow{Point=n.Label,Suppliers=parents.Count==0?"External source / inlet":string.Join(" + ",parents),Meter=id.HasValue&&active.TryGetValue(id.Value,out var meter)?meter.MeterName:"Meter not linked",CoveredDays=rows.Select(d=>d.ReadingDate).Distinct().Count(),Flow=rows.Count>0?rows.Sum(d=>d.Usage):null};
  }).ToList();
  return new(){Balances=balances,Transfers=transfers,FinalUse=finalScope,PointFlows=points};
 }
}
