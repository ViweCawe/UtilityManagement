using UtilityManagerProjects.Services;
using UtilityManagerProjects.Models;
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
        private readonly ConsumptionService calculations;
        private readonly decimal? carbonFactor;
        public bool CarbonEstimateAvailable => Consumption.Electricity.IsComplete && carbonFactor.HasValue;
        public bool CarbonPerPersonAvailable => CarbonEstimateAvailable && TotalPeople>0;
        public ConsumptionPeriod Consumption { get; private set; } = new();
        public bool WaterPerPersonAvailable => Consumption.Water.IsComplete && TotalPeople > 0;
        public bool ElectricityPerPersonAvailable => Consumption.Electricity.IsComplete && TotalPeople > 0;

        private const decimal WaterTargetPerPerson = 5.00m;
        private const decimal ElectricityTargetPerPerson = 1.00m;
        private const decimal RecyclingTargetPercent = 80.00m;
        private const decimal CarbonTargetPerPerson = 1.15m;

        public IndexModel(
            IMeterReadingData meterReadingData,
            IWasteReadingData wasteReadingData,
            IDailyPeopleCountData dailyPeopleCountData, ConsumptionService calculations, IConfiguration configuration)
        {
            this.meterReadingData = meterReadingData;
            this.wasteReadingData = wasteReadingData;
            this.dailyPeopleCountData = dailyPeopleCountData;
            this.calculations = calculations;
            var factor=configuration.GetValue<decimal?>("Environmental:GridEmissionFactorKgCo2ePerKwh");
            carbonFactor=factor>0?factor:null;
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

        public List<MeterReadingTableRow> MeterReadingRows { get; set; } = new();
        public List<WasteReadingTableRow> WasteReadingRows { get; set; } = new();

        public string TrendLabelsJson { get; set; } = "[]";
        public string WaterTrendJson { get; set; } = "[]";
        public string ElectricityTrendJson { get; set; } = "[]";
        public string RecyclingWasteTrendJson { get; set; } = "[]";
        public string LandfillWasteTrendJson { get; set; } = "[]";
        public string OtherWasteTrendJson { get; set; } = "[]";
        public string WasteSplitLabelsJson { get; set; } = "[]";
        public string WasteSplitDataJson { get; set; } = "[]";

        public async Task OnGet()
        {
            ResolveDateRange();

            await calculations.LoadAsync();
            Consumption = calculations.ForPeriod(CurrentStart, CurrentEnd);
            ViewData["Consumption"] = Consumption;
            ViewData["ConsumptionWarnings"] = calculations.Warnings;
            var meterReadings = calculations.History.Where(r=>IsInRange(r.ReadingDate,CurrentStart,CurrentEnd)).ToList();

            var wasteReadings = (await wasteReadingData.GetWasteReadingDisplay())
                .Where(x => x.IsDeleted == false && IsInRange(x.ReadingDate, CurrentStart, CurrentEnd))
                .ToList();

            var peopleCounts = (await dailyPeopleCountData.GetDailyPeopleCountsByDateRange(CurrentStart, CurrentEnd))
                .ToList();

            TotalVisitors = peopleCounts.Sum(x => x.Visitors);
            TotalEmployees = peopleCounts.Sum(x => x.Employees);
            TotalPeople = TotalVisitors + TotalEmployees;

            WaterUsage = Consumption.Water.Total;
            ElectricityUsage = Consumption.Electricity.Total;

            WasteTotal = wasteReadings.Sum(x => x.WasteReading);

            WaterPerPerson = WaterPerPersonAvailable ? CalculatePerPerson(WaterUsage, TotalPeople) : 0;
            ElectricityPerPerson = ElectricityPerPersonAvailable ? CalculatePerPerson(ElectricityUsage, TotalPeople) : 0;

            RecyclingWaste = wasteReadings
                .Where(x => GetWasteClassification(x) == "Recycling")
                .Sum(x => x.WasteReading);

            LandfillWaste = wasteReadings
                .Where(x => GetWasteClassification(x) == "Landfill")
                .Sum(x => x.WasteReading);

            OtherWaste = wasteReadings
                .Where(x => GetWasteClassification(x) == "Other")
                .Sum(x => x.WasteReading);

            RecyclingPercent = WasteTotal == 0
                ? 0
                : Math.Round((RecyclingWaste / WasteTotal) * 100, 1);

            EstimatedCarbonKg = CalculateEstimatedCarbon(ElectricityUsage);
            EstimatedCarbonPerPerson = CalculatePerPerson(EstimatedCarbonKg, TotalPeople);

            BuildAlerts();
            BuildTableRows(meterReadings, wasteReadings);
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
            var range=ConsumptionService.ReportingRange(CurrentStart,CurrentEnd);
            CurrentStart=range.Start; CurrentEnd=range.End;
            ViewData["ConsumptionDateNotice"]=range.Notice;

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

        private decimal CalculateEstimatedCarbon(decimal electricityKwh)
        {
            return CarbonEstimateAvailable ? Math.Round(electricityKwh*carbonFactor!.Value,2) : 0;
        }

        private static bool IsRecycling(WasteReadingDisplay item) => WasteCategoryClassifier.Classify(item.WasteCategory)==WasteDisposition.Recycling;
        private static bool IsLandfill(WasteReadingDisplay item) => WasteCategoryClassifier.Classify(item.WasteCategory)==WasteDisposition.Landfill;

        private static bool ContainsText(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetWasteClassification(WasteReadingDisplay item)
        {
            if (IsRecycling(item))
            {
                return "Recycling";
            }

            if (IsLandfill(item))
            {
                return "Landfill";
            }

            return "Other";
        }

        private static string DisplayValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
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

            if (WaterPerPersonAvailable && WaterPerPerson > WaterTargetPerPerson)
            {
                Alerts.Add(new EnvironmentalAlert
                {
                    Title = "Water supply indicator above target",
                    Message = $"{WaterPerPerson:N2} L supplied/person, not adjusted for stored-volume changes. Indicative target is {WaterTargetPerPerson:N2} L/person.",
                    Type = "danger"
                });
            }

            if (ElectricityPerPersonAvailable && ElectricityPerPerson > ElectricityTargetPerPerson)
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

            if (CarbonPerPersonAvailable && EstimatedCarbonPerPerson > CarbonTargetPerPerson)
            {
                Alerts.Add(new EnvironmentalAlert
                {
                    Title = "Carbon emissions above target",
                    Message = $"{EstimatedCarbonPerPerson:N2} kg/person estimated. Target is {CarbonTargetPerPerson:N2} kg/person.",
                    Type = "warning"
                });
            }

            if (!WaterPerPersonAvailable || !ElectricityPerPersonAvailable)
                Alerts.Add(new EnvironmentalAlert { Title="Incomplete meter interval coverage", Message="Per-person comparisons are unavailable unless the entire period has bracketed readings and a people count. Daily meter estimates are not actual day-by-day measurements.", Type="warning" });
            if (!carbonFactor.HasValue) Alerts.Add(new EnvironmentalAlert { Title="Carbon factor not configured", Message="Set an approved grid emissions factor to enable the carbon estimate. The old placeholder factor is no longer used.", Type="warning" });
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

        private void BuildTableRows(List<MeterReading> meterReadings, List<WasteReadingDisplay> wasteReadings)
        {
            MeterReadingRows = meterReadings
                .OrderByDescending(x => x.ReadingDate)
                .ThenBy(x => x.MeterType)
                .Select(x => new MeterReadingTableRow
                {
                    ReadingDate = x.ReadingDate,
                    MeterType = x.MeterType.ToString(),
                    Usage = x.Usage,
                    Unit = x.MeterType == MeterType.Water ? "L" : "kWh"
                })
                .ToList();

            WasteReadingRows = wasteReadings
                .OrderByDescending(x => x.ReadingDate)
                .Select(x => new WasteReadingTableRow
                {
                    ReadingDate = x.ReadingDate,
                    Category = DisplayValue(x.WasteCategory),
                    WasteType = DisplayValue(x.WasteTypeName),
                    Material = DisplayValue(x.WasteMaterial),
                    Classification = GetWasteClassification(x),
                    WeightKg = x.WasteReading
                })
                .ToList();
        }

        private void BuildTrendChartData(List<MeterReading> meterReadings, List<WasteReadingDisplay> wasteReadings)
        {
            var buckets = Enumerable.Range(0,(CurrentEnd-CurrentStart).Days+1).Select(i=>new TrendBucket { Start=CurrentStart.AddDays(i),End=CurrentStart.AddDays(i),Label=CurrentStart.AddDays(i).ToString("dd MMM yyyy") }).ToList();
            TrendLabelsJson = JsonSerializer.Serialize(buckets.Select(x=>x.Label));
            WaterTrendJson = JsonSerializer.Serialize(Consumption.Water.Series());
            ElectricityTrendJson = JsonSerializer.Serialize(Consumption.Electricity.Series());
            RecyclingWasteTrendJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                wasteReadings
                    .Where(x => IsInRange(x.ReadingDate, bucket.Start, bucket.End) &&
                                GetWasteClassification(x) == "Recycling")
                    .Sum(x => x.WasteReading)));

            LandfillWasteTrendJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                wasteReadings
                    .Where(x => IsInRange(x.ReadingDate, bucket.Start, bucket.End) &&
                                GetWasteClassification(x) == "Landfill")
                    .Sum(x => x.WasteReading)));

            OtherWasteTrendJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                wasteReadings
                    .Where(x => IsInRange(x.ReadingDate, bucket.Start, bucket.End) &&
                                GetWasteClassification(x) == "Other")
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
                "Wet Recycling"
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

        public class MeterReadingTableRow
        {
            public DateTime ReadingDate { get; set; }
            public string MeterType { get; set; } = string.Empty;
            public decimal Usage { get; set; }
            public string Unit { get; set; } = string.Empty;
        }

        public class WasteReadingTableRow
        {
            public DateTime ReadingDate { get; set; }
            public string Category { get; set; } = string.Empty;
            public string WasteType { get; set; } = string.Empty;
            public string Material { get; set; } = string.Empty;
            public string Classification { get; set; } = string.Empty;
            public decimal WeightKg { get; set; }
        }

        private class TrendBucket
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}