using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Analytics
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

        public List<AnalyticsRow> AnalyticsRows { get; set; } = new();

        public async Task OnGet()
        {
            var meterReadings = (await meterReadingData.GetAllMeterReading()).ToList();

            var wasteReadings = (await wasteReadingData.GetAllWasteReadings())
                .Where(x => x.IsDeleted == false)
                .ToList();

            var today = DateTime.Today;

            var current7Start = today.AddDays(-6);
            var previous7Start = today.AddDays(-13);
            var previous7End = today.AddDays(-7);

            var current30Start = today.AddDays(-29);
            var previous30Start = today.AddDays(-59);
            var previous30End = today.AddDays(-30);

            AnalyticsRows = new List<AnalyticsRow>
            {
                BuildMeterAnalytics("Water", "L", meterReadings, MeterType.Water, current7Start, previous7Start, previous7End, current30Start, previous30Start, previous30End),
                BuildMeterAnalytics("Electricity", "kWh", meterReadings, MeterType.Electricity, current7Start, previous7Start, previous7End, current30Start, previous30Start, previous30End),
                BuildWasteAnalytics("Waste", "kg", wasteReadings, current7Start, previous7Start, previous7End, current30Start, previous30Start, previous30End)
            };
        }

        private static AnalyticsRow BuildMeterAnalytics(
            string category,
            string unit,
            List<MeterReading> readings,
            MeterType meterType,
            DateTime current7Start,
            DateTime previous7Start,
            DateTime previous7End,
            DateTime current30Start,
            DateTime previous30Start,
            DateTime previous30End)
        {
            var current7 = readings
                .Where(x => x.MeterType == meterType && x.ReadingDate.Date >= current7Start)
                .Sum(x => x.Usage);

            var previous7 = readings
                .Where(x => x.MeterType == meterType && x.ReadingDate.Date >= previous7Start && x.ReadingDate.Date <= previous7End)
                .Sum(x => x.Usage);

            var current30 = readings
                .Where(x => x.MeterType == meterType && x.ReadingDate.Date >= current30Start)
                .Sum(x => x.Usage);

            var previous30 = readings
                .Where(x => x.MeterType == meterType && x.ReadingDate.Date >= previous30Start && x.ReadingDate.Date <= previous30End)
                .Sum(x => x.Usage);

            return new AnalyticsRow
            {
                Category = category,
                Unit = unit,
                Current7Days = current7,
                Previous7Days = previous7,
                WeeklyGrowthPercent = CalculateGrowth(current7, previous7),
                Current30Days = current30,
                Previous30Days = previous30,
                MonthlyGrowthPercent = CalculateGrowth(current30, previous30)
            };
        }

        private static AnalyticsRow BuildWasteAnalytics(
            string category,
            string unit,
            List<WasteReading> readings,
            DateTime current7Start,
            DateTime previous7Start,
            DateTime previous7End,
            DateTime current30Start,
            DateTime previous30Start,
            DateTime previous30End)
        {
            var current7 = readings
                .Where(x => x.ReadingDate.Date >= current7Start)
                .Sum(x => x.WasteAmount);

            var previous7 = readings
                .Where(x => x.ReadingDate.Date >= previous7Start && x.ReadingDate.Date <= previous7End)
                .Sum(x => x.WasteAmount);

            var current30 = readings
                .Where(x => x.ReadingDate.Date >= current30Start)
                .Sum(x => x.WasteAmount);

            var previous30 = readings
                .Where(x => x.ReadingDate.Date >= previous30Start && x.ReadingDate.Date <= previous30End)
                .Sum(x => x.WasteAmount);

            return new AnalyticsRow
            {
                Category = category,
                Unit = unit,
                Current7Days = current7,
                Previous7Days = previous7,
                WeeklyGrowthPercent = CalculateGrowth(current7, previous7),
                Current30Days = current30,
                Previous30Days = previous30,
                MonthlyGrowthPercent = CalculateGrowth(current30, previous30)
            };
        }

        private static double CalculateGrowth(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0)
            {
                return currentValue > 0 ? 100 : 0;
            }

            return Math.Round((double)((currentValue - previousValue) / previousValue * 100), 1);
        }

        public class AnalyticsRow
        {
            public string Category { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public decimal Current7Days { get; set; }
            public decimal Previous7Days { get; set; }
            public double WeeklyGrowthPercent { get; set; }
            public decimal Current30Days { get; set; }
            public decimal Previous30Days { get; set; }
            public double MonthlyGrowthPercent { get; set; }
        }
    }
}