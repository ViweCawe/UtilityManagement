using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Models;
using UtilityManagerProjects.Services;
namespace UtilityManagerProjects.Pages.WaterBalance;
public sealed class IndexModel(ConsumptionService service) : PageModel
{
 [BindProperty(SupportsGet=true)] public DateTime? StartDate { get; set; }
 [BindProperty(SupportsGet=true)] public DateTime? EndDate { get; set; }
 public ConsumptionPeriod Flow { get; private set; } = new();
 public WaterOutletMovementTable OutletTransfers { get; private set; } = new();
 public WaterConsumptionTable EndUse { get; private set; } = new();
 public WaterFlowTraceReport Trace { get; private set; } = new();
 public WaterStorageReport Report { get; private set; } = new();
 public async Task OnGetAsync() {
  var dates=ConsumptionService.ReportingRange(StartDate??DateTime.Today.AddDays(-6),EndDate??DateTime.Today);
  StartDate=dates.Start;EndDate=dates.End;ViewData["DateNotice"]=dates.Notice;
  await service.LoadAsync();Flow=service.ForPeriod(dates.Start,dates.End);EndUse=service.ConsumptionTable(Flow);OutletTransfers=service.OutletMovements(Flow);Report=service.StorageForPeriod(dates.Start,dates.End);Trace=service.FlowTraceForPeriod(dates.Start,dates.End);
 }
}
