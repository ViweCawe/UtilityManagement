using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Meters
{
    public class IndexModel : PageModel
    {
        private readonly IMeterData meterData;

        public IndexModel(IMeterData meterData)
        {
            this.meterData = meterData;
        }

        public int WaterCount { get; set; }
        public int ElectricityCount { get; set; }
        public int TotalCount { get; set; }

        public double WaterPercent { get; set; }
        public double ElectricityPercent { get; set; }

        public IEnumerable<Meter> MeterList { get; set; } = Enumerable.Empty<Meter>();
        public List<MeterTypeKpi> MeterTypeKpis { get; set; } = new();
        public List<MeterRow> MeterRows { get; set; } = new();

        public async Task OnGet()
        {
            var meters = (await meterData.GetMeters()).ToList();

            MeterList = meters;

            TotalCount = meters.Count;
            WaterCount = meters.Count(x => x.MeterType == MeterType.Water);
            ElectricityCount = meters.Count(x => x.MeterType == MeterType.Electricity);

            WaterPercent = CalculatePercent(WaterCount, TotalCount);
            ElectricityPercent = CalculatePercent(ElectricityCount, TotalCount);

            foreach (var type in Enum.GetValues<MeterType>())
            {
                var meterTypeCount = meters.Count(x => x.MeterType == type);

                MeterTypeKpis.Add(new MeterTypeKpi
                {
                    MeterType = type,
                    Current7Days = meterTypeCount,
                    Current30Days = meterTypeCount
                });
            }

            MeterRows = meters
                .Select((x, index) => new MeterRow
                {
                    Reference = $"MTR-{index + 1:0000}",
                    MeterName = x.MeterName,
                    MeterId = x.Id.ToString(),
                    MeterType = x.MeterType.ToString(),
                    Status = "Active"
                })
                .ToList();
        }

        private static double CalculatePercent(int value, int total)
        {
            if (total == 0)
            {
                return 0;
            }

            return Math.Round((value * 100.0) / total, 1);
        }

        public class MeterRow
        {
            public string Reference { get; set; } = string.Empty;
            public string MeterName { get; set; } = string.Empty;
            public string MeterId { get; set; } = string.Empty;
            public string MeterType { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
    }
}