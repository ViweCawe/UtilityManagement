using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics.Metrics;

namespace UtilityManagerProjects.Pages.WasteReadings
{
    public class CreateModel : PageModel
    {
        private readonly IWasteReadingData wasteReadingData;
        private readonly IWasteTypeData wasteTypeData;
        private readonly IEmployeeData employeeData;
        private readonly UserManager<IdentityUser> userManager;

        public CreateModel(IWasteReadingData wasteDataReading, IWasteTypeData wasteTypeData, IEmployeeData employeeData, UserManager<IdentityUser> userManager)
        {
            this.wasteReadingData = wasteDataReading;
            this.wasteTypeData = wasteTypeData;
            this.employeeData = employeeData;
            this.userManager = userManager;
        }

  

        public class InputModel()
        {
            public int WasteTypeId { get; set; }
            public decimal WasteAmount { get; set; }
            public string Notes { get; set; } = string.Empty;

        }
        public List<SelectListItem> WasteTypeList { get; set; } = new List<SelectListItem>();

        [BindProperty]
        public WasteReading WasteRecord { get; set; } = new();

        public async Task OnGetAsync()
        {
            var wasteTypes = await wasteTypeData.GetWasteTypes();
            WasteTypeList = new List<SelectListItem>();

            wasteTypes.ForEach(x =>
            {
                WasteTypeList.Add(new SelectListItem
                {
                    Text = x.WasteTypeName,
                    Value = x.Id.ToString()
                });
            });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            
            if (!ModelState.IsValid)
            {
                return Page();
            }


            var userId = userManager.GetUserId(User);
            var employee = await employeeData.GetEmployeeByUserId(userId);
            var wasteTypes = await wasteTypeData.GetWasteTypes();

             if(wasteTypes == null || !wasteTypes.Any(t => t.Id == WasteRecord.WasteTypeId))
            {
                ModelState.AddModelError(string.Empty ,"Selected Waste type not found ");
                return Page();
            }


            if (userId == null)
            {
                ModelState.AddModelError("", "Employee not found.");
                return Page();
            }

            var latestReading = await wasteReadingData.GetWasteReadingById(WasteRecord.WasteTypeId);

            WasteRecord.CreatedBy = employee.Id; // make sure this exists in model
            WasteRecord.ReadingDate = DateTime.Now;
            var id=await wasteReadingData.CreateWasteReading(WasteRecord);

            return RedirectToPage("Display", new {id});
        }
       
    }
}