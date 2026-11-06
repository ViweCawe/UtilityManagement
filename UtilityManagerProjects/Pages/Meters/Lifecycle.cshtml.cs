using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Models;
using UtilityManagerProjects.Services;
namespace UtilityManagerProjects.Pages.Meters;
public class LifecycleModel(IMeterData meterData, IMeterReadingData readingData) : PageModel
{
 [BindProperty(SupportsGet=true)] public int Id { get; set; }
 [BindProperty(SupportsGet=true)] public DateTime? StartDate { get; set; }
 [BindProperty(SupportsGet=true)] public DateTime? EndDate { get; set; }
 public List<Meter> Family { get; private set; }=[];
 public List<MeterReading> Captures { get; private set; }=[];
 public List<MonthRow> Months { get; private set; }=[];
 public List<decimal?> Series { get; private set; }=[];
 public string Unit { get; private set; }="L";
 public List<string> Issues { get; }=[];
 public async Task<IActionResult> OnGetAsync()
 {
  var all=await meterData.GetMeters();if(!all.Any(m=>m.Id==Id))return NotFound();
  var lineage=new MeterLineage(all);Family=lineage.Family(Id);Unit=MeterUnits.Default(Family[0].MeterType);
  var range=ConsumptionService.ReportingRange(StartDate??DateTime.Today.AddYears(-5),EndDate??DateTime.Today);StartDate=range.Start;EndDate=range.End;
  var ids=Family.Select(m=>m.Id).ToHashSet();var captures=(await readingData.GetAllMeterReading()).Where(r=>ids.Contains(r.MeterId)&&!r.IsDeleted&&r.ReadingDate.Date<=DateTime.Today).ToList();
  foreach(var r in captures) {var meter=Family.Single(m=>m.Id==r.MeterId);r.Unit=meter.Unit;r.MeterType=meter.MeterType;}
  if(captures.Any(r=>!lineage.InService(r)))Issues.Add("Some captures fall outside the recorded physical service dates. They are listed but excluded from this trend.");
  var valid=captures.Where(lineage.InService).ToList();
  var daily=IntervalConsumption.Allocate(valid,range.Start,range.End);
  var values=daily.GroupBy(d=>d.ReadingDate).ToDictionary(g=>g.Key,g=>g.Sum(d=>d.Usage));
  Series=Enumerable.Range(0,(range.End-range.Start).Days+1).Select(i=>values.TryGetValue(range.Start.AddDays(i),out var value)?(decimal?)value:null).ToList();
  Months=Enumerable.Range(0,(range.End.Year-range.Start.Year)*12+range.End.Month-range.Start.Month+1).Select(i=>new DateTime(range.Start.Year,range.Start.Month,1).AddMonths(i)).Select(month=>{
   var start=month<range.Start?range.Start:month;var monthEnd=month.AddMonths(1).AddDays(-1);var end=monthEnd>range.End?range.End:monthEnd;var rows=daily.Where(d=>d.ReadingDate>=start&&d.ReadingDate<=end).ToList();
   return new MonthRow(month,rows.Sum(d=>d.Usage),rows.Select(d=>d.ReadingDate).Distinct().Count(),(end-start).Days+1);
  }).ToList();
  Captures=captures.Where(r=>r.ReadingDate.Date>=range.Start&&r.ReadingDate.Date<=range.End).OrderByDescending(r=>r.ReadingDate).ThenByDescending(r=>r.Id).Take(1000).ToList();
  if(valid.GroupBy(r=>r.MeterId).Any(g=>g.OrderBy(r=>r.ReadingDate).Zip(g.OrderBy(r=>r.ReadingDate).Skip(1)).Any(p=>p.Second.CurrentReading<p.First.CurrentReading)))Issues.Add("A physical counter decreases. That interval is excluded; verify the capture or record the replacement correctly.");
  return Page();
 }
 public record MonthRow(DateTime Month,decimal Volume,int CoveredDays,int Days);
}
