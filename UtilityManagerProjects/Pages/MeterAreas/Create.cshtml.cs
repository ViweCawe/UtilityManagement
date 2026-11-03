using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace UtilityManagerProjects.Pages.MeterAreas;
[Authorize(Roles="Admin")]
public sealed class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Area/AreaIndex");
    public IActionResult OnPost() => RedirectToPage("/Area/AreaIndex");
}
