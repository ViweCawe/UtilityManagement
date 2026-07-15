using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Usage
{
    public class IndexModel : PageModel
    {
        private readonly IMeterReadingData meterReadingData;
        private readonly IWasteReadingData wasteReadingData;

        public IndexModel(IMeterReadingData meterReadingData, IWasteReadingData wasteReadingData)
        {
            this.meterReadingData = meterReadingData;
            this.wasteReadingData = wasteReadingData;
        }

        public List<UsageRow> UsageRows { get; set; } = new();

        public async Task OnGet()
        {
            var meterReadings = (await meterReadingData.GetAllMeterReading()).ToList();

            var wasteReadings = (await wasteReadingData.GetAllWasteReadings())
                .Where(x => x.IsDeleted == false)
                .ToList();

            var today = DateTime.Today;
            var current7Start = today.AddDays(-6);
            var current30Start = today.AddDays(-29);

            UsageRows = new List<UsageRow>
            {
                new UsageRow
                {
                    Category = "Water",
                    Unit = "L",
                    Total = meterReadings.Where(x => x.MeterType == MeterType.Water).Sum(x => x.Usage),
                    Current7Days = meterReadings.Where(x => x.MeterType == MeterType.Water && x.ReadingDate.Date >= current7Start).Sum(x => x.Usage),
                    Current30Days = meterReadings.Where(x => x.MeterType == MeterType.Water && x.ReadingDate.Date >= current30Start).Sum(x => x.Usage)
                },
                new UsageRow
                {
                    Category = "Electricity",
                    Unit = "kWh",
                    Total = meterReadings.Where(x => x.MeterType == MeterType.Electricity).Sum(x => x.Usage),
                    Current7Days = meterReadings.Where(x => x.MeterType == MeterType.Electricity && x.ReadingDate.Date >= current7Start).Sum(x => x.Usage),
                    Current30Days = meterReadings.Where(x => x.MeterType == MeterType.Electricity && x.ReadingDate.Date >= current30Start).Sum(x => x.Usage)
                },
                new UsageRow
                {
                    Category = "Waste",
                    Unit = "kg",
                    Total = wasteReadings.Sum(x => x.WasteAmount),
                    Current7Days = wasteReadings.Where(x => x.ReadingDate.Date >= current7Start).Sum(x => x.WasteAmount),
                    Current30Days = wasteReadings.Where(x => x.ReadingDate.Date >= current30Start).Sum(x => x.WasteAmount)
                }
            };
        }

        public class UsageRow
        {
            public string Category { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public decimal Current7Days { get; set; }
            public decimal Current30Days { get; set; }
        }
    }
}