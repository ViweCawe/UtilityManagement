using UtilityManagerProjects.Services;
using UtilityManagerProjects.Models;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Analytics
{
    public class IndexModel : PageModel
    {
        private readonly IMeterReadingData meterReadingData;
        private readonly IWasteReadingData wasteReadingData;
        private readonly ConsumptionService calculations;
        public ConsumptionPeriod Consumption { get; private set; } = new();

        public IndexModel(IMeterReadingData meterReadingData, IWasteReadingData wasteReadingData, ConsumptionService calculations)
        {
            this.meterReadingData = meterReadingData;
            this.wasteReadingData = wasteReadingData;
            this.calculations = calculations;
        }

        public List<AnalyticsRow> AnalyticsRows { get; set; } = new();

        public async Task OnGet()
        {
            await calculations.LoadAsync();
            var today = DateTime.Today;
            Consumption = calculations.ForPeriod(today.AddDays(-29), today);
            ViewData["Consumption"] = Consumption;
            ViewData["ConsumptionWarnings"] = calculations.Warnings;

            var wasteReadings = (await wasteReadingData.GetAllWasteReadings())
                .Where(x => x.IsDeleted == false)
                .ToList();

            var current7Start = today.AddDays(-6);
            var previous7Start = today.AddDays(-13);
            var previous7End = today.AddDays(-7);

            var current30Start = today.AddDays(-29);
            var previous30Start = today.AddDays(-59);
            var previous30End = today.AddDays(-30);

            var current7 = calculations.ForPeriod(current7Start,today);
            var previous7 = calculations.ForPeriod(previous7Start,previous7End);
            var previous30 = calculations.ForPeriod(previous30Start,previous30End);
            AnalyticsRows = new List<AnalyticsRow>
            {
                new AnalyticsRow { Category="Main Meter municipal supply",Unit="L",HasCurrent7=current7.Water.CoveredDays>0,HasPrevious7=previous7.Water.CoveredDays>0,HasCurrent30=Consumption.Water.CoveredDays>0,HasPrevious30=previous30.Water.CoveredDays>0,Current7Days=current7.Water.Total,Previous7Days=previous7.Water.Total,WeeklyGrowthPercent=current7.Water.IsComplete && previous7.Water.IsComplete ? CalculateGrowth(current7.Water.Total,previous7.Water.Total):0,Current30Days=Consumption.Water.Total,Previous30Days=previous30.Water.Total,MonthlyGrowthPercent=Consumption.Water.IsComplete&&previous30.Water.IsComplete?CalculateGrowth(Consumption.Water.Total,previous30.Water.Total):0,
                    WeeklyAvailable=current7.Water.IsComplete&&previous7.Water.IsComplete&&previous7.Water.Total>0, MonthlyAvailable=Consumption.Water.IsComplete&&previous30.Water.IsComplete&&previous30.Water.Total>0 },
                new AnalyticsRow { Category="Electricity",Unit="kWh",HasCurrent7=current7.Electricity.CoveredDays>0,HasPrevious7=previous7.Electricity.CoveredDays>0,HasCurrent30=Consumption.Electricity.CoveredDays>0,HasPrevious30=previous30.Electricity.CoveredDays>0,Current7Days=current7.Electricity.Total,Previous7Days=previous7.Electricity.Total,WeeklyGrowthPercent=current7.Electricity.IsComplete&&previous7.Electricity.IsComplete?CalculateGrowth(current7.Electricity.Total,previous7.Electricity.Total):0,Current30Days=Consumption.Electricity.Total,Previous30Days=previous30.Electricity.Total,MonthlyGrowthPercent=Consumption.Electricity.IsComplete&&previous30.Electricity.IsComplete?CalculateGrowth(Consumption.Electricity.Total,previous30.Electricity.Total):0,
                    WeeklyAvailable=current7.Electricity.IsComplete&&previous7.Electricity.IsComplete&&previous7.Electricity.Total>0,MonthlyAvailable=Consumption.Electricity.IsComplete&&previous30.Electricity.IsComplete&&previous30.Electricity.Total>0 },
                BuildWasteAnalytics("Waste", "kg", wasteReadings, current7Start, previous7Start, previous7End, current30Start, previous30Start, previous30End)
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
                return 0;
            }

            return Math.Round((double)((currentValue - previousValue) / previousValue * 100), 1);
        }

        public class AnalyticsRow
        {
            public bool HasCurrent7 { get; set; } = true;
            public bool HasPrevious7 { get; set; } = true;
            public bool HasCurrent30 { get; set; } = true;
            public bool HasPrevious30 { get; set; } = true;
            public bool WeeklyAvailable { get; set; } = true;
            public bool MonthlyAvailable { get; set; } = true;
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