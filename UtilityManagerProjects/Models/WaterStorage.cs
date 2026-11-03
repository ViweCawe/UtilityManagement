using DataLibrary.Models;
namespace UtilityManagerProjects.Models;

public sealed class WaterStorageBalance
{
 public string NodeId { get; init; } = "";
 public string Name { get; init; } = "";
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
 public static WaterStorageReport Calculate(WaterNetwork map,IReadOnlyDictionary<string,int?> links,List<Meter> meters,List<MeterReading> history,DateTime start,DateTime end)
 {
  start=start.Date;end=end.Date;
  var active=meters.Where(m=>m.IsActive&&m.MeterType==MeterType.Water).ToDictionary(m=>m.Id);
  var allocations=IntervalConsumption.Allocate(history.Where(h=>active.ContainsKey(h.MeterId)),start,end);
  ConsumptionScope Scope(IEnumerable<int> ids,DateTime from,DateTime to,List<DailyUsage>? rows=null) => new(){Start=from,End=to,MeterIds=ids.ToHashSet(),Allocated=rows??allocations};
  var finalIds=new HashSet<int>();var missingFinal=new List<string>();
  foreach(var node in map.Nodes.Where(n=>n.MeasuresFinalUse)) {if(links.TryGetValue(node.Id,out var id)&&id.HasValue&&active.ContainsKey(id.Value)){
    if(map.Nodes.Any(n=>n.Storage?.InletMeterIds.Contains(id.Value)==true)) missingFinal.Add(node.Label+": this counter credits storage and cannot also be final use.");else finalIds.Add(id.Value);
   }else missingFinal.Add("Link the final-use meter: "+node.Label);}
  var finalScope=new ConsumptionScope{Start=start,End=end,MeterIds=finalIds,Missing=missingFinal,Allocated=allocations,Label="Recorded final use at confirmed consumption meters"};
  var balances=new List<WaterStorageBalance>();
  var storageNodes=map.Nodes.Where(n=>n.Kind=="storage"||n.Storage is not null||WaterAccounting.Role(n)=="main").ToList();
  foreach(var node in storageNodes) {
   var row=new WaterStorageBalance{NodeId=node.Id,Name=node.Label};balances.Add(row);var settings=node.Storage;
   if(settings is null){row.Status=node.Kind=="source"?"Supply counter only. No physical stored volume has been defined.":"Storage boundaries and opening volume are not configured.";continue;}
   row.OpeningDate=settings.OpeningDate?.Date;row.OpeningVolume=settings.OpeningVolumeLitres;
   if(row.OpeningDate==end && row.OpeningVolume.HasValue) {row.CalculatedStock=row.OpeningVolume;row.Status="Measured opening stock at end of this date.";continue;}
   var all=settings.InletMeterIds.Concat(settings.OutletMeterIds).ToHashSet();
   if(settings.InletMeterIds.Count==0||settings.OutletMeterIds.Count==0){row.Status="Identify measured inlet and outlet counters. Missing outflow is not zero consumption.";continue;}
   if(all.Any(id=>!active.ContainsKey(id)||!links.Any(p=>p.Value==id&&map.Nodes.Any(n=>n.Id==p.Key)))){row.Status="A boundary meter is inactive or not linked on the map.";continue;}
   var parentOrigins=WaterAccounting.Origins(map,node.Id);
   if(settings.OutletMeterIds.Any(id=>links.Where(p=>p.Value==id).SelectMany(p=>WaterAccounting.Origins(map,p.Key)).Any(origin=>!parentOrigins.Contains(origin)))){row.Status="An outlet counter also includes another source. Its combined reading cannot be debited to this single-source tank.";continue;}
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
