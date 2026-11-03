using System.Data.Common;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace UtilityManagerProjects.Pages.WasteReadings;
public sealed class CreateModel(IWasteReadingData readings,IWasteTypeData types,IEmployeeData employees,UserManager<IdentityUser> users) : PageModel
{
 [BindProperty] public WasteReading WasteRecord { get; set; } = new();
 public List<SelectListItem> WasteTypeList { get; private set; } = [];
 async Task LoadTypes() => WasteTypeList=(await types.GetWasteTypes()).Where(t=>t.IsActive).Select(t=>new SelectListItem(t.WasteTypeName,t.Id.ToString())).ToList();
 public async Task OnGetAsync() => await LoadTypes();
 public async Task<IActionResult> OnPostAsync() {
  await LoadTypes();
  if(!WasteTypeList.Any(t=>t.Value==WasteRecord.WasteTypeId.ToString())) ModelState.AddModelError("WasteRecord.WasteTypeId","Select an active waste type.");
  if(WasteRecord.ReadingDate.Date<new DateTime(1753,1,1)||WasteRecord.ReadingDate.Date>DateTime.Today) ModelState.AddModelError("WasteRecord.ReadingDate","Choose a collection date no later than today.");
  if(decimal.Round(WasteRecord.WasteAmount,2)!=WasteRecord.WasteAmount) ModelState.AddModelError("WasteRecord.WasteAmount","Use at most two decimal places.");
  if(!ModelState.IsValid) return Page();
  var userId=users.GetUserId(User);if(string.IsNullOrEmpty(userId)) return Challenge();
  var employee=await employees.GetEmployeeByUserId(userId);
  if(employee is null||!employee.IsActive) {ModelState.AddModelError("","No active employee record is linked to your account. Ask an administrator to check the account link.");return Page();}
  WasteRecord.CreatedBy=employee.Id;
  try {var id=await readings.CreateWasteReading(WasteRecord);return RedirectToPage("./Display",new{id});}
  catch(DbException) {ModelState.AddModelError("","The collection could not be saved. Reload and check the selected type, amount and employee link.");return Page();}
 }
}
