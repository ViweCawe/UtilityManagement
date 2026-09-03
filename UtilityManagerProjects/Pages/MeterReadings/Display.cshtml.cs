using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using UtilityManagerProjects.Models;

namespace UtilityManagerProjects.Pages.MeterReadings
{
    public class DisplayModel : PageModel
    {
        private readonly IMeterReadingData meterReadingData;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IEmployeeData employeeData;

        public DisplayModel(IMeterReadingData meterReadingData, UserManager<IdentityUser> userManager, IEmployeeData employeeData)
        {
            this.meterReadingData = meterReadingData;
            this.userManager = userManager;
            this.employeeData = employeeData;
        }

        public MeterReading? Reading { get; set; }

        [BindProperty]
        public MeterReadingUpdate ReadingUpdate { get; set; } = new();

        public async Task<IActionResult> OnGet(int id)
        {
            Reading = await meterReadingData.GetMeterReadingsById(id);

            if (Reading == null)
            {
                return RedirectToPage("./List");
            }

            ReadingUpdate = new MeterReadingUpdate
            {
                Id = Reading.Id,
                CurrentReadingUpdate = Reading.CurrentReading,
                Notes = Reading.Notes,
                ReadingDateUpdate = Reading.ReadingDate

            };

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                Reading = await meterReadingData
                    .GetMeterReadingsById(ReadingUpdate.Id);

                return Page();
            }

            var identityUserId =
                userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return Challenge();
            }

            var employee =
                await employeeData
                    .GetEmployeeByUserId(identityUserId);

            if (employee == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No employee record is linked to the signed-in user.");

                Reading = await meterReadingData
                    .GetMeterReadingsById(ReadingUpdate.Id);

                return Page();
            }

            await meterReadingData.UpdateMeterReadings(
                ReadingUpdate.Id,

                ReadingUpdate.CurrentReadingUpdate,
                ReadingUpdate.Notes ?? string.Empty,
                employee.Id,
                ReadingUpdate.ReadingDateUpdate) ;

            return RedirectToPage(
                "./Display",
                new { id = ReadingUpdate.Id });
        }


    }
}