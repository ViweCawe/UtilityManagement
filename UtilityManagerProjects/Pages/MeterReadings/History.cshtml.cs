using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.MeterReadings
{
    public class HistoryModel : PageModel
    {
        private readonly IMeterReadingData meterReadingData;

        public HistoryModel(IMeterReadingData meterReadingData)
        {
            this.meterReadingData = meterReadingData;
        }

        [BindProperty(SupportsGet = true)]
        public int MeterId { get; set; }

        public MeterReading? LatestReading { get; set; }

        public List<MeterReading> ReadingHistory { get; set; } = new();

        public int TotalReadings { get; set; }

        public decimal TotalUsage { get; set; }

        public decimal HighestUsage { get; set; }

        public decimal LowestUsage { get; set; }

        public async Task<IActionResult> OnGet()
        {
            if (MeterId <= 0)
            {
                return RedirectToPage("./List");
            }

            ReadingHistory = (await meterReadingData.GetMeterReadingsByMeterId(MeterId))
                .OrderByDescending(x => x.ReadingDate)
                .ToList();

            LatestReading = ReadingHistory.FirstOrDefault();

            if (LatestReading == null)
            {
                return RedirectToPage("./List");
            }

            TotalReadings = ReadingHistory.Count;
            TotalUsage = ReadingHistory.Sum(x => x.Usage);
            HighestUsage = ReadingHistory.Max(x => x.Usage);
            LowestUsage = ReadingHistory.Min(x => x.Usage);

            return Page();
        }

        public string GetMeterUnit()
        {
            if (LatestReading == null)
            {
                return string.Empty;
            }

            return LatestReading.MeterType == MeterType.Water ? "L" : "kWh";
        }

        public string MeterDisplayName()
        {
            if (LatestReading == null)
            {
                return $"Meter {MeterId}";
            }

            return !string.IsNullOrWhiteSpace(LatestReading.MeterName)
                ? LatestReading.MeterName
                : $"Meter {LatestReading.MeterId}";
        }
    }
}