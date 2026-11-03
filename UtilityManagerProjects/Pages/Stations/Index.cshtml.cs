using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Services;
namespace UtilityManagerProjects.Pages.Stations;
[Authorize(Roles="Admin")]
public sealed class IndexModel(ReferenceDataStore store) : PageModel
{
 public List<ReferenceDataStore.Row> Rows { get; private set; } = [];
 public async Task OnGetAsync() => Rows=await store.Locations();
 public async Task<IActionResult> OnPostDeleteAsync(int id) {
  try { await store.DeleteLocation(id);TempData["SuccessMessage"]="Record deleted."; }
  catch(DbException ex) { TempData["ErrorMessage"]=Admin.ReferenceEditorModel.ReferenceError(ex); }
  return RedirectToPage();
 }
}
