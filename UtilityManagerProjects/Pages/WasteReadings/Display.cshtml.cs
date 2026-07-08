using DataLibrary.Data;
using DataLibrary.Db;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis;
using UtilityManagerProjects.Models;

namespace UtilityManagerProjects.Pages.WasteReadings
{
    public class DisplayModel : PageModel
    {
        private readonly IWasteReadingData wasteReading;
        private readonly ConnectionStringData stringData;
        private readonly IWasteTypeData wasteTypeData;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public WasteReadingUpdate WasteRecordUpdate { get; set; }
        [BindProperty]
        public WasteReading WasteRecord { get; set; }

        public string WasteRecorded{ get; set; }
        public DisplayModel(IWasteReadingData wasteReading,ConnectionStringData stringData,IWasteTypeData wasteTypeData)
        {
            this.wasteReading = wasteReading;
            this.stringData = stringData;
            this.wasteTypeData = wasteTypeData;
        }

        public async Task<IActionResult> OnGet()
        {
            ViewData["HideNavbar"] = true;
            WasteRecord = await wasteTypeData.GetWasteRecordsById(Id);

            if (WasteRecord != null)
            {
                // Initialize the update model so Razor can read its properties safely
                WasteRecordUpdate = new WasteReadingUpdate
                {
                    Id = WasteRecord.Id,
                    wasteReadingUpdate = WasteRecord.WasteAmount,
                    Notes = WasteRecord.Notes,
                    UpdatedAt = WasteRecord.UpdatedAt,
                    UpdatedBy = WasteRecord.UpdatedBy ?? 0

                };

                var wasteTypes = await wasteTypeData.GetWasteTypes();

                WasteRecorded = wasteTypes
                    .Where(x => x.Id == WasteRecord.WasteTypeId)
                    .FirstOrDefault()?.WasteTypeName;
            }
            return Page();
        }
    }
}
