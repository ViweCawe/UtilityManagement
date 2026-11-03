using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace UtilityManagerProjects.Areas.Identity.Pages.Account;
[Authorize(Roles="Admin")]
public sealed class RegisterModel : PageModel
{
 public IActionResult OnGet() => RedirectToPage("/Admin/UserManagement/Edit",new {area=""});
 public IActionResult OnPost() => RedirectToPage("/Admin/UserManagement/Edit",new {area=""});
}
