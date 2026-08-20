using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace UtilityManagerProjects.Pages.WasteReadings
{
    [Authorize(Roles = "Admin")]
    public class ListModel : PageModel
    {
        private readonly IWasteReadingData wasteReading;

        public ListModel(IWasteReadingData wasteReading)
        {
            this.wasteReading = wasteReading;
        }

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; } = "Last30";

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public DateTime CurrentStart { get; set; }
        public DateTime CurrentEnd { get; set; }
        public DateTime PreviousStart { get; set; }
        public DateTime PreviousEnd { get; set; }

        public string DateRangeLabel => $"{CurrentStart:dd MMM yyyy} - {CurrentEnd:dd MMM yyyy}";

        public int TotalReadings { get; set; }
        public int DeletedReadings { get; set; }
        public int TotalWasteTypes { get; set; }
        public int TotalCategories { get; set; }
        public int TotalMaterials { get; set; }

        public decimal TotalWasteGenerated { get; set; }
        public decimal PreviousWasteGenerated { get; set; }
        public double WasteGrowthPercent { get; set; }

        public decimal Current7DaysWaste { get; set; }
        public decimal Previous7DaysWaste { get; set; }
        public double WeeklyGrowthPercent { get; set; }

        public decimal Current30DaysWaste { get; set; }
        public decimal Previous30DaysWaste { get; set; }
        public double MonthlyGrowthPercent { get; set; }

        public decimal AverageWastePerReading { get; set; }

        public decimal RecyclingWaste { get; set; }
        public decimal LandfillWaste { get; set; }
        public decimal OtherWaste { get; set; }
        public decimal RecyclingPercent { get; set; }

        public string TopWasteType { get; set; } = "N/A";
        public string TopWasteCategory { get; set; } = "N/A";
        public string TopWasteMaterial { get; set; } = "N/A";

        public List<WasteReadingDisplay> WasteReadings { get; set; } = new();
        public List<WasteSummaryRow> WasteTypeSummary { get; set; } = new();
        public List<WasteSummaryRow> WasteCategorySummary { get; set; } = new();
        public List<WasteSummaryRow> WasteMaterialSummary { get; set; } = new();
        public List<WasteReadingDisplay> LatestReadings { get; set; } = new();
        public string DailyLabelsJson { get; set; } = "[]";
        public string DailyWasteJson { get; set; } = "[]";

        public string CategoryLabelsJson { get; set; } = "[]";
        public string CategoryDataJson { get; set; } = "[]";

        public string MaterialLabelsJson { get; set; } = "[]";
        public string MaterialDataJson { get; set; } = "[]";

        public string WasteSplitLabelsJson { get; set; } = "[]";
        public string WasteSplitDataJson { get; set; } = "[]";

        public async Task OnGet()
        {
            ResolveDateRange();

            var allRows = (await wasteReading.GetWasteReadingDisplay()).ToList();

            DeletedReadings = allRows.Count(x => x.IsDeleted);

            var activeRows = allRows
                .Where(x => x.IsDeleted == false)
                .ToList();

            var currentRows = activeRows
                .Where(x => IsInRange(x.ReadingDate, CurrentStart, CurrentEnd))
                .ToList();

            var previousRows = activeRows
                .Where(x => IsInRange(x.ReadingDate, PreviousStart, PreviousEnd))
                .ToList();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var search = SearchTerm.Trim();

                currentRows = currentRows
                    .Where(x =>
                        ContainsText(x.Id.ToString(), search) ||
                        ContainsText(x.WasteTypeId.ToString(), search) ||
                        ContainsText(x.WasteTypeName, search) ||
                        ContainsText(x.WasteCategory, search) ||
                        ContainsText(x.WasteMaterial, search) ||
                        ContainsText(x.CapturedBy.ToString(), search) ||
                        ContainsText(x.Notes, search))
                    .ToList();
            }
            LatestReadings = currentRows
                .GroupBy(x => x.WasteTypeId)
                .Select(group => group
                    .OrderByDescending(x => x.ReadingDate)
                    .ThenByDescending(x => x.Id)
                    .First())
                .OrderByDescending(x => x.ReadingDate)
                .ToList();

            WasteReadings = currentRows
                .OrderByDescending(x => x.ReadingDate)
                .ToList();

            TotalReadings = WasteReadings.Count;
            TotalWasteGenerated = WasteReadings.Sum(x => x.WasteAmount);
            PreviousWasteGenerated = previousRows.Sum(x => x.WasteAmount);
            WasteGrowthPercent = CalculateGrowth(TotalWasteGenerated, PreviousWasteGenerated);

            TotalWasteTypes = WasteReadings
                .Select(x => x.WasteTypeId)
                .Distinct()
                .Count();

            TotalCategories = WasteReadings
                .Select(x => x.WasteCategory)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Count();

            TotalMaterials = WasteReadings
                .Select(x => x.WasteMaterial)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Count();

            AverageWastePerReading = TotalReadings == 0
                ? 0
                : Math.Round(TotalWasteGenerated / TotalReadings, 2);

            Current7DaysWaste = GetWasteTotal(activeRows, CurrentEnd.AddDays(-6), CurrentEnd);
            Previous7DaysWaste = GetWasteTotal(activeRows, CurrentEnd.AddDays(-13), CurrentEnd.AddDays(-7));
            WeeklyGrowthPercent = CalculateGrowth(Current7DaysWaste, Previous7DaysWaste);

            Current30DaysWaste = GetWasteTotal(activeRows, CurrentEnd.AddDays(-29), CurrentEnd);
            Previous30DaysWaste = GetWasteTotal(activeRows, CurrentEnd.AddDays(-59), CurrentEnd.AddDays(-30));
            MonthlyGrowthPercent = CalculateGrowth(Current30DaysWaste, Previous30DaysWaste);

            RecyclingWaste = WasteReadings
                .Where(IsRecycling)
                .Sum(x => x.WasteAmount);

            LandfillWaste = WasteReadings
                .Where(IsLandfill)
                .Sum(x => x.WasteAmount);

            OtherWaste = TotalWasteGenerated - RecyclingWaste - LandfillWaste;

            RecyclingPercent = TotalWasteGenerated == 0
                ? 0
                : Math.Round((RecyclingWaste / TotalWasteGenerated) * 100, 1);

            WasteTypeSummary = WasteReadings
                .GroupBy(x => string.IsNullOrWhiteSpace(x.WasteTypeName) ? $"Type {x.WasteTypeId}" : x.WasteTypeName)
                .Select(group => new WasteSummaryRow
                {
                    Name = group.Key,
                    ReadingCount = group.Count(),
                    TotalAmount = group.Sum(x => x.WasteAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            WasteCategorySummary = WasteReadings
                .GroupBy(x => string.IsNullOrWhiteSpace(x.WasteCategory) ? "Unknown Category" : x.WasteCategory)
                .Select(group => new WasteSummaryRow
                {
                    Name = group.Key,
                    ReadingCount = group.Count(),
                    TotalAmount = group.Sum(x => x.WasteAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            WasteMaterialSummary = WasteReadings
                .GroupBy(x => string.IsNullOrWhiteSpace(x.WasteMaterial) ? "Unknown Material" : x.WasteMaterial)
                .Select(group => new WasteSummaryRow
                {
                    Name = group.Key,
                    ReadingCount = group.Count(),
                    TotalAmount = group.Sum(x => x.WasteAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            TopWasteType = WasteTypeSummary.FirstOrDefault()?.Name ?? "N/A";
            TopWasteCategory = WasteCategorySummary.FirstOrDefault()?.Name ?? "N/A";
            TopWasteMaterial = WasteMaterialSummary.FirstOrDefault()?.Name ?? "N/A";

            BuildDailyWasteChart();
            BuildCategoryChart();
            BuildMaterialChart();
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

            var days = (CurrentEnd - CurrentStart).Days + 1;

            PreviousEnd = CurrentStart.AddDays(-1);
            PreviousStart = PreviousEnd.AddDays(-(days - 1));
        }

        private static bool IsInRange(DateTime date, DateTime startDate, DateTime endDate)
        {
            return date.Date >= startDate.Date && date.Date <= endDate.Date;
        }

        private static bool ContainsText(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetWasteTotal(List<WasteReadingDisplay> readings, DateTime startDate, DateTime endDate)
        {
            return readings
                .Where(x => IsInRange(x.ReadingDate, startDate, endDate))
                .Sum(x => x.WasteAmount);
        }

        private static double CalculateGrowth(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0)
            {
                return currentValue > 0 ? 100 : 0;
            }

            return Math.Round((double)((currentValue - previousValue) / previousValue * 100), 1);
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

        private void BuildDailyWasteChart()
        {
            var buckets = BuildTrendBuckets(CurrentStart, CurrentEnd);

            DailyLabelsJson = JsonSerializer.Serialize(buckets.Select(x => x.Label));

            DailyWasteJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                WasteReadings
                    .Where(x => IsInRange(x.ReadingDate, bucket.Start, bucket.End))
                    .Sum(x => x.WasteAmount)));
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
                var yearStart = new DateTime(year, 1, 1);
                var yearEnd = new DateTime(year, 12, 31);

                if (yearStart < startDate.Date)
                {
                    yearStart = startDate.Date;
                }

                if (yearEnd > endDate.Date)
                {
                    yearEnd = endDate.Date;
                }

                buckets.Add(new TrendBucket
                {
                    Start = yearStart,
                    End = yearEnd,
                    Label = year.ToString()
                });
            }

            return buckets;
        }

        private void BuildCategoryChart()
        {
            var topCategories = WasteCategorySummary
                .Take(5)
                .ToList();

            CategoryLabelsJson = JsonSerializer.Serialize(topCategories.Select(x => x.Name));
            CategoryDataJson = JsonSerializer.Serialize(topCategories.Select(x => x.TotalAmount));
        }

        private void BuildMaterialChart()
        {
            var topMaterials = WasteMaterialSummary
                .Take(5)
                .ToList();

            MaterialLabelsJson = JsonSerializer.Serialize(topMaterials.Select(x => x.Name));
            MaterialDataJson = JsonSerializer.Serialize(topMaterials.Select(x => x.TotalAmount));
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

        public class WasteSummaryRow
        {
            public string Name { get; set; } = string.Empty;
            public int ReadingCount { get; set; }
            public decimal TotalAmount { get; set; }
        }

        private class TrendBucket
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}