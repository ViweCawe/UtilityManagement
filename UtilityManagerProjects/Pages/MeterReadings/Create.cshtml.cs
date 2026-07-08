using DataLibrary.Data;
using DataLibrary.Db;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;

namespace UtilityManagerProjects.Pages.MeterReadings
{
    public class CreateModel : PageModel
    {
        private readonly IMeterReadingData meterReadingData;
        private readonly IMeterData meterData;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IEmployeeData employeeData;

        public CreateModel(IMeterReadingData meterReadingData, IMeterData meterData,UserManager<IdentityUser> userManager,IEmployeeData employeeData)
        {
            this.meterReadingData = meterReadingData;
            this.meterData = meterData;
            this.userManager = userManager;
            this.employeeData = employeeData;
        }

        public List<SelectListItem> MeterItems { get; set; } = new List<SelectListItem>();
        [BindProperty]
        public MeterReading Reading { get; set; } 

        public class InputModel
        {
            public int MeterId { get; set; }
            [BindProperty]
            public decimal CurrentReading { get; set; }
            public string Notes { get; set; } = string.Empty;

        }
        public async Task OnGetAsync()
        {
            ViewData["HideNavbar"] = true;
            var meters = await meterData.GetMeters();
            MeterItems = new List<SelectListItem>();

            meters.ForEach(x =>
            {
                MeterItems.Add(new SelectListItem
                {
                    Text = x.MeterName,
                    Value =x.Id.ToString()
                });
            } );
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var meters = await meterData.GetMeters();

            if(meters == null || !meters.Any(m => m.Id == Reading.MeterId))
            {
                ModelState.AddModelError(string.Empty, "Selected meter does not exist.");
                return Page();
            }
            var userId = userManager.GetUserId(User);
            var employeeId = await employeeData.GetEmployeeByUserId(userId);

            if(employeeId == null)
            {
               ModelState.AddModelError(string.Empty, "Employee not found for the current user.");
                return Page();
            }


            //Last Readings
            var lastReading = await meterReadingData.GetLatestReadingByMeterId(Reading.MeterId);

            var previouse  = lastReading?.CurrentReading ?? 0;
            Reading.PreviousReading = previouse;
            Reading.CurrentReading = Reading.CurrentReading;
            Reading.Usage = Reading.CurrentReading - Reading.PreviousReading;
            Reading.ReadingDate = DateTime.Now;
            Reading.EmployeeId = employeeId.Id;
            Reading.Notes = Reading.Notes ?? string.Empty;
            Reading.MeterType = meters.FirstOrDefault(m => m.Id == Reading.MeterId)?.MeterType ?? MeterType.Electricity;


            Console.WriteLine($"EmployeeId = {Reading.EmployeeId}");
            int id = await meterReadingData.CreateMeterReadings(Reading);

            return RedirectToPage("Display", new { id });

        }
    }
}
