using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages
{
    public class PrivacyModel : PageModel
    {
        public void OnGet()
        {
            ViewData["HideSidebar"] = true;
        }
    }

}
