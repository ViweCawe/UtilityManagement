using DataLibrary.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;

namespace UtilityManagerProjects.Pages.MeterAreas
{
    public class CreateModel : PageModel
    {
        private readonly IAreaData areaData;
        private readonly IMeterData meterData;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IEmployeeData employeeData;

        public CreateModel(IAreaData areaData, IMeterData meterData, UserManager<IdentityUser> userManager, IEmployeeData employeeData)
        {
            this.areaData = areaData;
            this.meterData = meterData;
            this.userManager = userManager;
            this.employeeData = employeeData;
        }
        public void OnGet()
        {
            ViewData["HideNavbar"] = true;
        }
    }
}
