using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Reports
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

        public int TotalMeterReadings { get; set; }
        public int TotalWasteReadings { get; set; }
        public int TotalReadings { get; set; }

        public decimal WaterTotal { get; set; }
        public decimal ElectricityTotal { get; set; }
        public decimal WasteTotal { get; set; }

        public DateTime GeneratedOn { get; set; } = DateTime.Now;

        public async Task OnGet()
        {
            var meterReadings = (await meterReadingData.GetAllMeterReading()).ToList();

            var wasteReadings = (await wasteReadingData.GetAllWasteReadings())
                .Where(x => x.IsDeleted == false)
                .ToList();

            TotalMeterReadings = meterReadings.Count;
            TotalWasteReadings = wasteReadings.Count;
            TotalReadings = TotalMeterReadings + TotalWasteReadings;

            WaterTotal = meterReadings
                .Where(x => x.MeterType == MeterType.Water)
                .Sum(x => x.Usage);

            ElectricityTotal = meterReadings
                .Where(x => x.MeterType == MeterType.Electricity)
                .Sum(x => x.Usage);

            WasteTotal = wasteReadings.Sum(x => x.WasteAmount);
        }
    }
}