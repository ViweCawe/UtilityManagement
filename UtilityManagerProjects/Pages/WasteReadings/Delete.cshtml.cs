using System.Data.Common;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace UtilityManagerProjects.Pages.WasteReadings;
public sealed class DeleteModel(IWasteReadingData readings,IEmployeeData employees,UserManager<IdentityUser> users) : PageModel
{
 [BindProperty(SupportsGet=true)] public int Id { get; set; }
 public WasteReading? Record { get; private set; }
 public async Task<IActionResult> OnGetAsync() {Record=await readings.GetWasteReadingById(Id);return Record is null||Record.IsDeleted?NotFound():Page();}
 public async Task<IActionResult> OnPostAsync() {
  Record=await readings.GetWasteReadingById(Id);if(Record is null||Record.IsDeleted)return NotFound();
  var userId=users.GetUserId(User);if(string.IsNullOrEmpty(userId))return Challenge();
  var employee=await employees.GetEmployeeByUserId(userId);
  if(employee is null||!employee.IsActive){ModelState.AddModelError("","No active employee record is linked to your account.");return Page();}
  try {await readings.DeleteWasteReading(Id,employee.Id);TempData["SuccessMessage"]="Waste collection deleted.";return RedirectToPage("./List");}
  catch(DbException){ModelState.AddModelError("","Unable to delete this collection. Reload and try again.");return Page();}
 }
}
