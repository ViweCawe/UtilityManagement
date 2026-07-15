using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace UtilityManagerProjects.Pages.Dashboard
{
    public class DashboardModel : PageModel
    {
        private readonly IMeterReadingData readingData;

        public DashboardModel(IMeterReadingData readingData)
        {
            this.readingData = readingData;
        }

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; } = "Last30";

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public DateTime CurrentStart { get; set; }
        public DateTime CurrentEnd { get; set; }
        public DateTime PreviousStart { get; set; }
        public DateTime PreviousEnd { get; set; }

        public string DateRangeLabel => $"{CurrentStart:dd MMM yyyy} - {CurrentEnd:dd MMM yyyy}";

        public int WaterConsumption { get; set; }
        public int ElectricityConsumption { get; set; }

        public int PreviousWaterConsumption { get; set; }
        public int PreviousElectricityConsumption { get; set; }

        public double WaterWeeklyGrowthPercent { get; set; }
        public double ElectricityWeeklyGrowthPercent { get; set; }

        public int WaterCurrent30Days { get; set; }
        public int ElectricityCurrent30Days { get; set; }

        public int WaterReadingCount { get; set; }
        public int ElectricityReadingCount { get; set; }
        public int TotalMeterReadingCount { get; set; }

        public List<MeterReading> MeterReadings { get; set; } = new();
        public List<MeterReading> RecentMeterReadings { get; set; } = new();

        public List<MeterTypeKpi> MeterTypeKpis { get; set; } = new();

        public List<DashboardAlert> Alerts { get; set; } = new();

        public List<AreaConsumptionRow> TopWaterAreas { get; set; } = new();
        public List<AreaConsumptionRow> TopElectricityAreas { get; set; } = new();

        public string TrendLabelsJson { get; set; } = "[]";
        public string WaterTrendJson { get; set; } = "[]";
        public string ElectricityTrendJson { get; set; } = "[]";

        public string MonthLabelsJson { get; set; } = "[]";
        public string MonthlyWaterJson { get; set; } = "[]";
        public string MonthlyElectricityJson { get; set; } = "[]";

        public string DistributionLabelsJson { get; set; } = "[]";
        public string DistributionDataJson { get; set; } = "[]";

        public async Task OnGet()
        {
            ResolveDateRange();

            var currentReadings = (await readingData.GetMeterReadingsByDateRange(CurrentStart, CurrentEnd))
                .Where(x => x.MeterType == MeterType.Water || x.MeterType == MeterType.Electricity)
                .ToList();

            var previousReadings = (await readingData.GetMeterReadingsByDateRange(PreviousStart, PreviousEnd))
                .Where(x => x.MeterType == MeterType.Water || x.MeterType == MeterType.Electricity)
                .ToList();

            var last60Start = CurrentEnd.AddDays(-59);

            var last60Readings = (await readingData.GetMeterReadingsByDateRange(last60Start, CurrentEnd))
                .Where(x => x.MeterType == MeterType.Water || x.MeterType == MeterType.Electricity)
                .ToList();

            MeterReadings = currentReadings;

            WaterConsumption = GetMeterTotal(currentReadings, MeterType.Water);
            ElectricityConsumption = GetMeterTotal(currentReadings, MeterType.Electricity);

            PreviousWaterConsumption = GetMeterTotal(previousReadings, MeterType.Water);
            PreviousElectricityConsumption = GetMeterTotal(previousReadings, MeterType.Electricity);

            WaterWeeklyGrowthPercent = CalculateGrowth(WaterConsumption, PreviousWaterConsumption);
            ElectricityWeeklyGrowthPercent = CalculateGrowth(ElectricityConsumption, PreviousElectricityConsumption);

            WaterReadingCount = currentReadings.Count(x => x.MeterType == MeterType.Water);
            ElectricityReadingCount = currentReadings.Count(x => x.MeterType == MeterType.Electricity);
            TotalMeterReadingCount = WaterReadingCount + ElectricityReadingCount;

            MeterTypeKpis = new List<MeterTypeKpi>
            {
                BuildMeterTypeKpi(last60Readings, MeterType.Water, CurrentEnd),
                BuildMeterTypeKpi(last60Readings, MeterType.Electricity, CurrentEnd)
            };

            WaterCurrent30Days = MeterTypeKpis.First(x => x.MeterType == MeterType.Water).Current30Days;
            ElectricityCurrent30Days = MeterTypeKpis.First(x => x.MeterType == MeterType.Electricity).Current30Days;

            RecentMeterReadings = currentReadings
                .OrderByDescending(x => x.ReadingDate)
                .Take(5)
                .ToList();

            TopWaterAreas = BuildTopAreas(currentReadings, MeterType.Water);
            TopElectricityAreas = BuildTopAreas(currentReadings, MeterType.Electricity);

            BuildAlerts();
            BuildTrendChartData(currentReadings);
            BuildMonthlyChartData(last60Readings, CurrentEnd);
            BuildDistributionChartData();
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

        private static int GetMeterTotal(List<MeterReading> meterReadings, MeterType meterType)
        {
            return meterReadings
                .Where(x => x.MeterType == meterType)
                .Sum(x => x.Usage);
        }

        private static int GetMeterTotal(List<MeterReading> meterReadings, MeterType meterType, DateTime startDate, DateTime endDate)
        {
            return meterReadings
                .Where(x => x.MeterType == meterType && IsInRange(x.ReadingDate, startDate, endDate))
                .Sum(x => x.Usage);
        }

        private static double CalculateGrowth(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0)
            {
                return currentValue > 0 ? 100 : 0;
            }

            return Math.Round((double)((currentValue - previousValue) / previousValue * 100), 1);
        }

        private static MeterTypeKpi BuildMeterTypeKpi(List<MeterReading> meterReadings, MeterType meterType, DateTime periodEnd)
        {
            var current7Start = periodEnd.AddDays(-6);
            var previous7Start = periodEnd.AddDays(-13);
            var previous7End = periodEnd.AddDays(-7);

            var current30Start = periodEnd.AddDays(-29);
            var previous30Start = periodEnd.AddDays(-59);
            var previous30End = periodEnd.AddDays(-30);

            var current7Days = GetMeterTotal(meterReadings, meterType, current7Start, periodEnd);
            var previous7Days = GetMeterTotal(meterReadings, meterType, previous7Start, previous7End);

            var current30Days = GetMeterTotal(meterReadings, meterType, current30Start, periodEnd);
            var previous30Days = GetMeterTotal(meterReadings, meterType, previous30Start, previous30End);

            return new MeterTypeKpi
            {
                MeterType = meterType,
                Current7Days = current7Days,
                Previous7Days = previous7Days,
                WeeklyGrowthPercent = CalculateGrowth(current7Days, previous7Days),
                Current30Days = current30Days,
                Previous30Days = previous30Days,
                MonthlyGrowthPercent = CalculateGrowth(current30Days, previous30Days)
            };
        }

        private void BuildAlerts()
        {
            AddIncreaseAlert(
                "electricity consumption",
                PreviousElectricityConsumption,
                ElectricityConsumption,
                ElectricityWeeklyGrowthPercent,
                "kWh");

            AddIncreaseAlert(
                "water consumption",
                PreviousWaterConsumption,
                WaterConsumption,
                WaterWeeklyGrowthPercent,
                "L");

            if (!Alerts.Any())
            {
                Alerts.Add(new DashboardAlert
                {
                    Title = "No major alerts detected",
                    Message = "No 15% increase was found for the selected date range.",
                    Type = "success"
                });
            }
        }

        private void AddIncreaseAlert(string name, decimal previousValue, decimal currentValue, double growthPercent, string unit)
        {
            if (growthPercent >= 15)
            {
                Alerts.Add(new DashboardAlert
                {
                    Title = $"Increase in {name}",
                    Message = $"{previousValue:N0} {unit} → {currentValue:N0} {unit}. Increase: {growthPercent:N1}%.",
                    Type = "danger"
                });
            }
        }

        private void BuildTrendChartData(List<MeterReading> meterReadings)
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

        private void BuildMonthlyChartData(List<MeterReading> meterReadings, DateTime referenceDate)
        {
            var currentMonth = new DateTime(referenceDate.Year, referenceDate.Month, 1);
            var previousMonth = currentMonth.AddMonths(-1);

            var months = new List<DateTime>
            {
                previousMonth,
                currentMonth
            };

            MonthLabelsJson = JsonSerializer.Serialize(months.Select(x => x.ToString("MMM yyyy")));

            MonthlyWaterJson = JsonSerializer.Serialize(months.Select(month =>
                meterReadings
                    .Where(x => x.MeterType == MeterType.Water &&
                                x.ReadingDate.Year == month.Year &&
                                x.ReadingDate.Month == month.Month)
                    .Sum(x => x.Usage)));

            MonthlyElectricityJson = JsonSerializer.Serialize(months.Select(month =>
                meterReadings
                    .Where(x => x.MeterType == MeterType.Electricity &&
                                x.ReadingDate.Year == month.Year &&
                                x.ReadingDate.Month == month.Month)
                    .Sum(x => x.Usage)));
        }

        private static List<AreaConsumptionRow> BuildTopAreas(List<MeterReading> meterReadings, MeterType meterType)
        {
            var rows = meterReadings
                .Where(x => x.MeterType == meterType)
                .GroupBy(x => string.IsNullOrWhiteSpace(x.AreaName) ? "Unknown Area" : x.AreaName)
                .Select(group => new AreaConsumptionRow
                {
                    AreaName = group.Key,
                    TotalUsage = group.Sum(x => x.Usage)
                })
                .OrderByDescending(x => x.TotalUsage)
                .Take(3)
                .ToList();

            var max = rows.Any() ? rows.Max(x => x.TotalUsage) : 0;

            foreach (var row in rows)
            {
                row.ProgressPercent = max == 0
                    ? 0
                    : Convert.ToInt32(Math.Round((row.TotalUsage * 100.0) / max, 0));
            }

            return rows;
        }

        private void BuildDistributionChartData()
        {
            DistributionLabelsJson = JsonSerializer.Serialize(new[]
            {
                "Water",
                "Electricity"
            });

            DistributionDataJson = JsonSerializer.Serialize(new decimal[]
            {
                WaterConsumption,
                ElectricityConsumption
            });
        }

        public class DashboardAlert
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Type { get; set; } = "info";
        }

        public class AreaConsumptionRow
        {
            public string AreaName { get; set; } = string.Empty;
            public int TotalUsage { get; set; }
            public int ProgressPercent { get; set; }
        }

        private class TrendBucket
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}