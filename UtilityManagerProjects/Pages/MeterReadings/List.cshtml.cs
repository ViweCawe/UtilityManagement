using DataLibrary.Data;
using DataLibrary.Db;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Threading.Tasks;

namespace UtilityManagerProjects.Pages.MeterReadings
{
    public class ListModel : PageModel
    {
        private readonly IMeterReadingData meterReading;
        private readonly IWasteReadingData wasteReading;

        public ListModel(IMeterReadingData meterReading, IWasteReadingData wasteReading)
        {
            this.meterReading = meterReading;
            this.wasteReading = wasteReading;
        }
        public int TotalReadings { get; set; }
        public int TotalWasteReadings { get; set; }
        public int WaterCount { get; set; }
        public int ElectricityCount { get; set; }
        public int RefuseCount { get; set; }
        public int TotalMeters { get; set; }
        public int TotalWasteTypes { get; set; }
        public double WaterPercent { get; set; }
        public double ElectricityPercent { get; set; }
        public double RefusePercent { get; set; }
        public Meter Meter { get; set; }
        public IEnumerable<MeterReading> MeterReadings { get; set; }
        public IEnumerable<WasteReading> WasteReadings { get; set; }
        public List<MeterTypeKpi> MeterTypeKpis { get; set; } = new List<MeterTypeKpi>();


        public async Task OnGet()
        {
            ViewData["HideNavbar"] = true;
            MeterReadings = await meterReading.GetAllMeterReading();
            WasteReadings = await wasteReading.GetAllWasteReadings();


            TotalReadings = MeterReadings.Count();
            TotalWasteReadings = WasteReadings.Count();
            WaterCount = MeterReadings.Count(x => x.MeterType == MeterType.Water);
            ElectricityCount = MeterReadings.Count(x => x.MeterType == MeterType.Electricity);

            TotalMeters = MeterReadings
            .Select(x => x.MeterId)
            .Distinct()
            .Count();

            TotalWasteTypes = WasteReadings
                .Select(x => x.WasteTypeId)
                .Distinct()
                .Count();


            var now = DateTime.Now;

            var last7 = now.AddDays(-7);
            var last14 = now.AddDays(-14);

            var last30 = now.AddDays(-30);
            var last60 = now.AddDays(-60);

            var previouse7Days = WasteReadings.Count(x => x.ReadingDate >= last7 && x.ReadingDate < last14);

            foreach (var type in Enum.GetValues<MeterType>()   )
            {
                // WEEKLY
                var current7 = MeterReadings.Count(x =>
                   x.MeterType == type &&
                   x.ReadingDate >= last7);

                var previous7 = MeterReadings.Count(x =>
                    x.MeterType == type &&
                    x.ReadingDate >= last14 &&
                    x.ReadingDate < last7);

                var weeklyGrowth = previous7 == 0
                    ? 100
                    : ((current7 - previous7) * 100.0) / previous7;

                // MONTHLY
                var current30 = MeterReadings.Count(x =>
                    x.MeterType == type &&
                    x.ReadingDate >= last30);

                var previous30 = MeterReadings.Count(x =>
                    x.MeterType == type &&
                    x.ReadingDate >= last60 &&
                    x.ReadingDate < last30);

                var monthlyGrowth = previous30 == 0
                    ? 100
                    : ((current30 - previous30) * 100.0) / previous30;

                var count = MeterReadings.Count();
             

                MeterTypeKpis.Add(new MeterTypeKpi
                {
                    MeterType = type,

                    Current7Days = current7,
                    Previous7Days = previous7,
                    WeeklyGrowthPercent = weeklyGrowth,

                    Current30Days = current30,
                    Previous30Days = previous30,
                    MonthlyGrowthPercent = monthlyGrowth,

                });
            }
        }
    }
}