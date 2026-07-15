using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.WasteReadings
{
    public class HistoryModel : PageModel
    {
        private readonly IWasteReadingData wasteReadingData;

        public HistoryModel(IWasteReadingData wasteReadingData)
        {
            this.wasteReadingData = wasteReadingData;
        }

        [BindProperty(SupportsGet = true)]
        public int WasteTypeId { get; set; }

        public string WasteTypeName { get; set; } = string.Empty;
        public List<WasteReadingDisplay> Readings { get; set; } = new();

        public decimal TotalWaste => Readings.Sum(x => x.WasteAmount);

        public DateTime? LatestReadingDate =>
            Readings.Count == 0 ? null : Readings.Max(x => x.ReadingDate);

        public async Task<IActionResult> OnGetAsync()
        {
            if (WasteTypeId <= 0)
            {
                return RedirectToPage("./List");
            }

            var allReadings = await wasteReadingData.GetWasteReadingDisplay();

            Readings = allReadings
                .Where(x => !x.IsDeleted && x.WasteTypeId == WasteTypeId)
                .OrderByDescending(x => x.ReadingDate)
                .ToList();

            WasteTypeName =
                Readings.FirstOrDefault()?.WasteTypeName
                ?? $"Waste Type {WasteTypeId}";

            return Page();
        }
    }
}
