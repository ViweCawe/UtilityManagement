using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Models;

namespace UtilityManagerProjects.Pages.WasteReadings
{
    public class DisplayModel : PageModel
    {
        private readonly IWasteReadingData wasteReadingData;
        private readonly IWasteTypeData wasteTypeData;
        private readonly IEmployeeData employeeData;
        private readonly UserManager<IdentityUser> userManager;

        public DisplayModel(
            IWasteReadingData wasteReadingData,
            IWasteTypeData wasteTypeData,
            IEmployeeData employeeData,
            UserManager<IdentityUser> userManager)
        {
            this.wasteReadingData = wasteReadingData;
            this.wasteTypeData = wasteTypeData;
            this.employeeData = employeeData;
            this.userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public WasteReading? WasteRecord { get; set; }

        [BindProperty]
        public WasteReadingUpdate WasteRecordUpdate { get; set; } = new();

        public string WasteRecorded { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["HideNavbar"] = true;

            return await LoadReadingAsync(Id);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadReadingAsync(WasteRecordUpdate.Id);

                return Page();
            }

            var identityUserId = userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return Challenge();
            }

            var employee =
                await employeeData.GetEmployeeByUserId(identityUserId);

            if (employee == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No employee record is linked to the signed-in user.");

                await LoadReadingAsync(WasteRecordUpdate.Id);

                return Page();
            }

            await wasteReadingData.UpdateWasteReadings(
                WasteRecordUpdate.Id,
                WasteRecordUpdate.WasteAmount,
                WasteRecordUpdate.Notes ?? string.Empty,
                employee.Id);

            TempData["SuccessMessage"] =
                "Waste reading updated successfully.";

            return RedirectToPage(
                "./Display",
                new { id = WasteRecordUpdate.Id });
        }

        private async Task<IActionResult> LoadReadingAsync(int id)
        {
            WasteRecord =
                await wasteReadingData.GetWasteReadingById(id);

            if (WasteRecord == null)
            {
                return RedirectToPage("./List");
            }

            WasteRecordUpdate = new WasteReadingUpdate
            {
                Id = WasteRecord.Id,
                WasteAmount = WasteRecord.WasteAmount,
                Notes = WasteRecord.Notes ?? string.Empty
            };

            var wasteTypes =
                await wasteTypeData.GetWasteTypes();

            WasteRecorded = wasteTypes
                .FirstOrDefault(x =>
                    x.Id == WasteRecord.WasteTypeId)
                ?.WasteTypeName
                ?? WasteRecord.WasteTypeName
                ?? "Unknown Waste Type";

            return Page();
        }
    }
}