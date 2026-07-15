using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace UtilityManagerProjects.Pages.Environmental
{
    public class IndexModel : PageModel
    {
        private readonly IMeterReadingData meterReadingData;
        private readonly IWasteReadingData wasteReadingData;
        private readonly IDailyPeopleCountData dailyPeopleCountData;

        private const decimal WaterTargetPerPerson = 5.00m;
        private const decimal ElectricityTargetPerPerson = 1.00m;
        private const decimal RecyclingTargetPercent = 80.00m;
        private const decimal CarbonTargetPerPerson = 1.15m;

        public IndexModel(
            IMeterReadingData meterReadingData,
            IWasteReadingData wasteReadingData,
            IDailyPeopleCountData dailyPeopleCountData)
        {
            this.meterReadingData = meterReadingData;
            this.wasteReadingData = wasteReadingData;
            this.dailyPeopleCountData = dailyPeopleCountData;
        }

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; } = "Last30";

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public DateTime CurrentStart { get; set; }
        public DateTime CurrentEnd { get; set; }

        public string DateRangeLabel => $"{CurrentStart:dd MMM yyyy} - {CurrentEnd:dd MMM yyyy}";

        public int TotalPeople { get; set; }
        public int TotalVisitors { get; set; }
        public int TotalEmployees { get; set; }

        public decimal WaterUsage { get; set; }
        public decimal ElectricityUsage { get; set; }
        public decimal WasteTotal { get; set; }

        public decimal WaterPerPerson { get; set; }
        public decimal ElectricityPerPerson { get; set; }

        public decimal WaterTarget { get; set; } = WaterTargetPerPerson;
        public decimal ElectricityTarget { get; set; } = ElectricityTargetPerPerson;
        public decimal RecyclingTarget { get; set; } = RecyclingTargetPercent;
        public decimal CarbonTarget { get; set; } = CarbonTargetPerPerson;

        public decimal RecyclingWaste { get; set; }
        public decimal LandfillWaste { get; set; }
        public decimal OtherWaste { get; set; }
        public decimal RecyclingPercent { get; set; }

        public decimal EstimatedCarbonKg { get; set; }
        public decimal EstimatedCarbonPerPerson { get; set; }

        public List<EnvironmentalAlert> Alerts { get; set; } = new();

        public string TrendLabelsJson { get; set; } = "[]";
        public string WaterTrendJson { get; set; } = "[]";
        public string ElectricityTrendJson { get; set; } = "[]";
        public string WasteTrendJson { get; set; } = "[]";
        public string WasteSplitLabelsJson { get; set; } = "[]";
        public string WasteSplitDataJson { get; set; } = "[]";

        public async Task OnGet()
        {
            ResolveDateRange();

            var meterReadings = (await meterReadingData.GetMeterReadingsByDateRange(CurrentStart, CurrentEnd))
                .Where(x => x.MeterType == MeterType.Water || x.MeterType == MeterType.Electricity)
                .ToList();

            var wasteReadings = (await wasteReadingData.GetWasteReadingDisplay())
                .Where(x => x.IsDeleted == false && IsInRange(x.ReadingDate, CurrentStart, CurrentEnd))
                .ToList();

            var peopleCounts = (await dailyPeopleCountData.GetDailyPeopleCountsByDateRange(CurrentStart, CurrentEnd))
                .ToList();

            TotalVisitors = peopleCounts.Sum(x => x.Visitors);
            TotalEmployees = peopleCounts.Sum(x => x.Employees);
            TotalPeople = TotalVisitors + TotalEmployees;

            WaterUsage = meterReadings
                .Where(x => x.MeterType == MeterType.Water)
                .Sum(x => x.Usage);

            ElectricityUsage = meterReadings
                .Where(x => x.MeterType == MeterType.Electricity)
                .Sum(x => x.Usage);

            WasteTotal = wasteReadings.Sum(x => x.WasteReading);

            WaterPerPerson = CalculatePerPerson(WaterUsage, TotalPeople);
            ElectricityPerPerson = CalculatePerPerson(ElectricityUsage, TotalPeople);

            RecyclingWaste = wasteReadings.Where(IsRecycling).Sum(x => x.WasteReading);
            LandfillWaste = wasteReadings.Where(IsLandfill).Sum(x => x.WasteReading);
            OtherWaste = WasteTotal - RecyclingWaste - LandfillWaste;

            RecyclingPercent = WasteTotal == 0
                ? 0
                : Math.Round((RecyclingWaste / WasteTotal) * 100, 1);

            EstimatedCarbonKg = CalculateEstimatedCarbon(ElectricityUsage);
            EstimatedCarbonPerPerson = CalculatePerPerson(EstimatedCarbonKg, TotalPeople);

            BuildAlerts();
            BuildTrendChartData(meterReadings, wasteReadings);
            BuildWasteSplitChart();
        }

        private void ResolveDateRange()
        {
            var today = DateTime.Today;

            if (DateFilter == "Custom" && StartDate.HasValue && EndDate.HasValue)
            {
                CurrentStart = StartDate.Value.Date;
                CurrentEnd = EndDate.Value.Date;
            }
            else if (DateFilter == "Last7")
            {
                CurrentStart = today.AddDays(-6);
                CurrentEnd = today;
            }
            else
            {
                DateFilter = "Last30";
                CurrentStart = today.AddDays(-29);
                CurrentEnd = today;
            }

            if (CurrentStart > CurrentEnd)
            {
                (CurrentStart, CurrentEnd) = (CurrentEnd, CurrentStart);
            }
        }

        private static bool IsInRange(DateTime date, DateTime startDate, DateTime endDate)
        {
            return date.Date >= startDate.Date && date.Date <= endDate.Date;
        }

        private static decimal CalculatePerPerson(decimal totalUsage, int people)
        {
            if (people <= 0)
            {
                return 0;
            }

            return Math.Round(totalUsage / people, 2);
        }

        private static decimal CalculateEstimatedCarbon(decimal electricityKwh)
        {
            // Placeholder estimate until you add a carbon-factor table.
            // You can later replace this with a value from your database.
            const decimal defaultGridEmissionFactorKgCo2ePerKwh = 1.00m;

            return Math.Round(electricityKwh * defaultGridEmissionFactorKgCo2ePerKwh, 2);
        }

        private static bool IsRecycling(WasteReadingDisplay item)
        {
            return ContainsText(item.WasteCategory, "recycl") ||
                   ContainsText(item.WasteTypeName, "recycl") ||
                   ContainsText(item.WasteMaterial, "recycl");
        }

        private static bool IsLandfill(WasteReadingDisplay item)
        {
            return ContainsText(item.WasteCategory, "landfill") ||
                   ContainsText(item.WasteTypeName, "landfill") ||
                   ContainsText(item.WasteMaterial, "landfill");
        }

        private static bool ContainsText(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private void BuildAlerts()
        {
            if (TotalPeople == 0)
            {
                Alerts.Add(new EnvironmentalAlert
                {
                    Title = "No people count captured",
                    Message = "Capture daily visitors and employees to calculate per-person usage.",
                    Type = "warning"
                });
            }

            if (WaterPerPerson > WaterTargetPerPerson)
            {
                Alerts.Add(new EnvironmentalAlert
                {
                    Title = "Water target exceeded",
                    Message = $"{WaterPerPerson:N2} L/person used. Target is {WaterTargetPerPerson:N2} L/person.",
                    Type = "danger"
                });
            }

            if (ElectricityPerPerson > ElectricityTargetPerPerson)
            {
                Alerts.Add(new EnvironmentalAlert
                {
                    Title = "Electricity target exceeded",
                    Message = $"{ElectricityPerPerson:N2} kWh/person used. Target is {ElectricityTargetPerPerson:N2} kWh/person.",
                    Type = "danger"
                });
            }

            if (RecyclingPercent < RecyclingTargetPercent && WasteTotal > 0)
            {
                Alerts.Add(new EnvironmentalAlert
                {
                    Title = "Recycling below target",
                    Message = $"{RecyclingPercent:N1}% recycling achieved. Target is {RecyclingTargetPercent:N0}%.",
                    Type = "warning"
                });
            }

            if (EstimatedCarbonPerPerson > CarbonTargetPerPerson && TotalPeople > 0)
            {
                Alerts.Add(new EnvironmentalAlert
                {
                    Title = "Carbon emissions above target",
                    Message = $"{EstimatedCarbonPerPerson:N2} kg/person estimated. Target is {CarbonTargetPerPerson:N2} kg/person.",
                    Type = "warning"
                });
            }

            if (!Alerts.Any())
            {
                Alerts.Add(new EnvironmentalAlert
                {
                    Title = "Environmental performance on track",
                    Message = "All calculated environmental metrics are within target.",
                    Type = "success"
                });
            }
        }

        private void BuildTrendChartData(List<MeterReading> meterReadings, List<WasteReadingDisplay> wasteReadings)
        {
            var buckets = BuildTrendBuckets(CurrentStart, CurrentEnd);

            TrendLabelsJson = JsonSerializer.Serialize(buckets.Select(x => x.Label));

            WaterTrendJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                meterReadings
                    .Where(x => x.MeterType == MeterType.Water && IsInRange(x.ReadingDate, bucket.Start, bucket.End))
                    .Sum(x => x.Usage)));

            ElectricityTrendJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                meterReadings
                    .Where(x => x.MeterType == MeterType.Electricity && IsInRange(x.ReadingDate, bucket.Start, bucket.End))
                    .Sum(x => x.Usage)));

            WasteTrendJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                wasteReadings
                    .Where(x => IsInRange(x.ReadingDate, bucket.Start, bucket.End))
                    .Sum(x => x.WasteReading)));
        }

        private List<TrendBucket> BuildTrendBuckets(DateTime startDate, DateTime endDate)
        {
            var days = (endDate.Date - startDate.Date).Days + 1;
            var buckets = new List<TrendBucket>();

            if (days <= 90)
            {
                for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
                {
                    buckets.Add(new TrendBucket
                    {
                        Start = day,
                        End = day,
                        Label = day.ToString("dd MMM")
                    });
                }

                return buckets;
            }

            if (days <= 365)
            {
                var weekStart = startDate.Date;

                while (weekStart <= endDate.Date)
                {
                    var weekEnd = weekStart.AddDays(6);

                    if (weekEnd > endDate.Date)
                    {
                        weekEnd = endDate.Date;
                    }

                    buckets.Add(new TrendBucket
                    {
                        Start = weekStart,
                        End = weekEnd,
                        Label = weekStart.ToString("dd MMM")
                    });

                    weekStart = weekEnd.AddDays(1);
                }

                return buckets;
            }

            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                buckets.Add(new TrendBucket
                {
                    Start = new DateTime(year, 1, 1),
                    End = new DateTime(year, 12, 31),
                    Label = year.ToString()
                });
            }

            return buckets;
        }

        private void BuildWasteSplitChart()
        {
            WasteSplitLabelsJson = JsonSerializer.Serialize(new[]
            {
                "Recycling",
                "Landfill",
                "Other"
            });

            WasteSplitDataJson = JsonSerializer.Serialize(new[]
            {
                RecyclingWaste,
                LandfillWaste,
                OtherWaste
            });
        }

        public class EnvironmentalAlert
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Type { get; set; } = "info";
        }

        private class TrendBucket
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}