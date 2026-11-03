using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace UtilityManagerProjects.Pages.Admin.Roles;
[Authorize(Roles="Admin")]
public sealed class IndexModel : PageModel
{
 public IActionResult OnGet() => RedirectToPage("/Admin/UserManagement/Index");
 public IActionResult OnPost() => RedirectToPage("/Admin/UserManagement/Index");
}
