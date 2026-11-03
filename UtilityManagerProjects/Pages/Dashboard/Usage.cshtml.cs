using UtilityManagerProjects.Services;
using UtilityManagerProjects.Models;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Usage
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

        public List<UsageRow> UsageRows { get; set; } = new();

        public async Task OnGet()
        {
            await calculations.LoadAsync();
            var today = DateTime.Today;
            Consumption = calculations.ForPeriod(today.AddDays(-29), today);
            ViewData["Consumption"] = Consumption;

            var wasteReadings = (await wasteReadingData.GetAllWasteReadings())
                .Where(x => x.IsDeleted == false)
                .ToList();

            var current7Start = today.AddDays(-6);
            var current30Start = today.AddDays(-29);

            var firstDate = calculations.History.Any() ? calculations.History.Min(r=>r.ReadingDate).Date : today;
            // All-history total is measured only over days with complete boundary coverage.
            if ((today-firstDate).Days>3660) firstDate=today.AddDays(-3660);
            var all = calculations.ForPeriod(firstDate,today);
            var week = calculations.ForPeriod(current7Start,today);
            UsageRows = new List<UsageRow>
            {
                new UsageRow { Category="Metered water supply",Unit="L",HasTotal=all.Water.CoveredDays>0,HasWeek=week.Water.CoveredDays>0,HasMonth=Consumption.Water.CoveredDays>0,Total=all.Water.Total,Current7Days=week.Water.Total,Current30Days=Consumption.Water.Total },
                new UsageRow { Category="Electricity",Unit="kWh",HasTotal=all.Electricity.CoveredDays>0,HasWeek=week.Electricity.CoveredDays>0,HasMonth=Consumption.Electricity.CoveredDays>0,Total=all.Electricity.Total,Current7Days=week.Electricity.Total,Current30Days=Consumption.Electricity.Total },
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
            public bool HasTotal { get; set; } = true;
            public bool HasWeek { get; set; } = true;
            public bool HasMonth { get; set; } = true;
            public string Category { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public decimal Current7Days { get; set; }
            public decimal Current30Days { get; set; }
        }
    }
}