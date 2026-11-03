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
            if(Id>0 && Id!=WasteRecordUpdate.Id) return BadRequest();
            var existing=await wasteReadingData.GetWasteReadingById(WasteRecordUpdate.Id);
            if(existing is null||existing.IsDeleted) return NotFound();
            if(WasteRecordUpdate.ReadingDate is not DateTime date || date.Date<new DateTime(1753,1,1)||date.Date>DateTime.Today) ModelState.AddModelError("WasteRecordUpdate.ReadingDate","Choose a collection date no later than today.");
            if(decimal.Round(WasteRecordUpdate.WasteAmount,2)!=WasteRecordUpdate.WasteAmount) ModelState.AddModelError("WasteRecordUpdate.WasteAmount","Use at most two decimal places.");
            if(!ModelState.IsValid) {await LoadReadingAsync(existing.Id,false);return Page();}

            var identityUserId = userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return Challenge();
            }

            var employee =
                await employeeData.GetEmployeeByUserId(identityUserId);

            if (employee == null || !employee.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No employee record is linked to the signed-in user.");

                await LoadReadingAsync(WasteRecordUpdate.Id,false);

                return Page();
            }

            try { await wasteReadingData.UpdateWasteReadings(
                WasteRecordUpdate.Id,
                WasteRecordUpdate.WasteAmount,
                WasteRecordUpdate.Notes ?? string.Empty,
                employee.Id,WasteRecordUpdate.ReadingDate);
            } catch(System.Data.Common.DbException) { ModelState.AddModelError("","The collection could not be updated. Reload and try again.");await LoadReadingAsync(existing.Id,false);return Page(); }

            TempData["SuccessMessage"] =
                "Waste reading updated successfully.";

            return RedirectToPage(
                "./Display",
                new { id = WasteRecordUpdate.Id });
        }

        private async Task<IActionResult> LoadReadingAsync(int id,bool populateInput=true)
        {
            WasteRecord =
                await wasteReadingData.GetWasteReadingById(id);

            if (WasteRecord == null || WasteRecord.IsDeleted)
            {
                return NotFound();
            }

            if(populateInput) WasteRecordUpdate = new WasteReadingUpdate
            {
                Id = WasteRecord.Id, ReadingDate=WasteRecord.ReadingDate,
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