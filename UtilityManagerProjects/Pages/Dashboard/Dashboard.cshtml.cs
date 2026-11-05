using UtilityManagerProjects.Services;
using UtilityManagerProjects.Models;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace UtilityManagerProjects.Pages.Dashboard
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly IMeterReadingData readingData;
        private readonly ConsumptionService calculations;
        public WaterConsumptionTable EndUse { get; private set; } = new();
        public ConsumptionPeriod Consumption { get; private set; } = new();
        public bool WaterChangeAvailable { get; private set; }
        public bool ElectricityChangeAvailable { get; private set; }
        public bool WaterMonthChangeAvailable { get; private set; }
        public bool ElectricityMonthChangeAvailable { get; private set; }
        public bool PreviousMonthWaterAvailable { get; private set; }
        public bool CurrentMonthWaterAvailable { get; private set; }
        public bool PreviousMonthElectricityAvailable { get; private set; }
        public bool CurrentMonthElectricityAvailable { get; private set; }
        public string WaterMonthValuesJson { get; private set; } = "[]";
        public string ElectricityMonthValuesJson { get; private set; } = "[]";
        public string MonthMethodology { get; private set; } = "";

        public DashboardModel(IMeterReadingData readingData, ConsumptionService calculations)
        {
            this.readingData = readingData;
            this.calculations = calculations;
        }

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; } = "Last30";

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)] public int? MeterId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Department { get; set; }
        public List<MeterReading> MeterOptions { get; set; } = new();
        public List<string> DepartmentOptions { get; set; } = new();

        public DateTime CurrentStart { get; set; }
        public DateTime CurrentEnd { get; set; }
        public DateTime PreviousStart { get; set; }
        public DateTime PreviousEnd { get; set; }

        public string DateRangeLabel => $"{CurrentStart:dd MMM yyyy} - {CurrentEnd:dd MMM yyyy}";

        public decimal WaterConsumption { get; set; }
        public decimal ElectricityConsumption { get; set; }

        public decimal PreviousWaterConsumption { get; set; }
        public decimal PreviousElectricityConsumption { get; set; }

        public double WaterWeeklyGrowthPercent { get; set; }
        public double ElectricityWeeklyGrowthPercent { get; set; }

        public decimal WaterCurrent30Days { get; set; }
        public decimal ElectricityCurrent30Days { get; set; }

        public decimal PreviousMonthWaterConsumption { get; set; }
        public decimal CurrentMonthWaterConsumption { get; set; }
        public decimal PreviousMonthElectricityConsumption { get; set; }
        public decimal CurrentMonthElectricityConsumption { get; set; }
        public double WaterMonthlyGrowthPercent { get; set; }
        public double ElectricityMonthlyGrowthPercent { get; set; }
        public string PreviousMonthLabel { get; set; } = string.Empty;
        public string CurrentMonthLabel { get; set; } = string.Empty;
        public string MonthComparisonLabelsJson { get; set; } = "[]";

        public int WaterReadingCount { get; set; }
        public int ElectricityReadingCount { get; set; }
        public int TotalMeterReadingCount { get; set; }

        public int SelectedDayCount { get; set; }
        public int CompleteCoverageDays { get; set; }
        public double DataCoveragePercent { get; set; }
        public double AverageDailyWater { get; set; }
        public double AverageDailyElectricity { get; set; }

        public List<MeterReading> MeterReadings { get; set; } = new();
        public List<MeterReading> RecentMeterReadings { get; set; } = new();

        public List<MeterDailyAverageRow> DailyAverages { get; private set; } = [];
        public List<MeterTypeKpi> MeterTypeKpis { get; set; } = new();

        public List<DashboardAlert> Alerts { get; set; } = new();

        public List<AreaConsumptionRow> TopWaterAreas { get; set; } = new();
        public List<AreaConsumptionRow> TopElectricityAreas { get; set; } = new();

        public string TrendLabelsJson { get; set; } = "[]";
        public string WaterTrendJson { get; set; } = "[]";
        public string ElectricityTrendJson { get; set; } = "[]";

        public string WaterAreaLabelsJson { get; set; } = "[]";
        public string WaterAreaDataJson { get; set; } = "[]";
        public string ElectricityAreaLabelsJson { get; set; } = "[]";
        public string ElectricityAreaDataJson { get; set; } = "[]";

        public async Task OnGet()
        {
            ResolveDateRange();

            await calculations.LoadAsync();
            var unfilteredCurrent = calculations.History;
            MeterOptions = unfilteredCurrent.GroupBy(x => x.MeterId).Select(x => x.First()).OrderBy(x => x.MeterName).ToList();
            DepartmentOptions = unfilteredCurrent.Select(x => x.DepartmentName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            Consumption = calculations.ForPeriod(CurrentStart, CurrentEnd, MeterId, Department);
            DailyAverages=calculations.DailyAverages(Consumption,MeterId,Department);
            EndUse = calculations.ConsumptionTable(Consumption, MeterId, Department);
            ViewData["Consumption"] = Consumption;
            ViewData["ConsumptionWarnings"] = calculations.Warnings;
            var previous = calculations.ForPeriod(PreviousStart, PreviousEnd, MeterId, Department);
            var currentReadings = Consumption.SummaryRows;
            var previousReadings = previous.SummaryRows;

            MeterReadings = ApplyMeterFilters(unfilteredCurrent).Where(r => IsInRange(r.ReadingDate, CurrentStart, CurrentEnd)).ToList();
            WaterChangeAvailable = Consumption.Water.IsComplete && previous.Water.IsComplete && previous.Water.Total > 0;
            ElectricityChangeAvailable = Consumption.Electricity.IsComplete && previous.Electricity.IsComplete && previous.Electricity.Total > 0;
            WaterConsumption = GetMeterTotal(currentReadings, MeterType.Water);
            ElectricityConsumption = GetMeterTotal(currentReadings, MeterType.Electricity);

            PreviousWaterConsumption = GetMeterTotal(previousReadings, MeterType.Water);
            PreviousElectricityConsumption = GetMeterTotal(previousReadings, MeterType.Electricity);

            WaterWeeklyGrowthPercent = WaterChangeAvailable ? CalculateGrowth(WaterConsumption, PreviousWaterConsumption) : 0;
            ElectricityWeeklyGrowthPercent = ElectricityChangeAvailable ? CalculateGrowth(ElectricityConsumption, PreviousElectricityConsumption) : 0;

            WaterReadingCount = MeterReadings.Count(x => x.MeterType == MeterType.Water);
            ElectricityReadingCount = MeterReadings.Count(x => x.MeterType == MeterType.Electricity);
            TotalMeterReadingCount = WaterReadingCount + ElectricityReadingCount;

            SelectedDayCount = Math.Max(1, (CurrentEnd.Date - CurrentStart.Date).Days + 1);
            CompleteCoverageDays = Consumption.CoveredDays;
            DataCoveragePercent = (double)Consumption.CoveragePercent;
            AverageDailyWater = (double)Consumption.Water.Average;
            AverageDailyElectricity = (double)Consumption.Electricity.Average;

            MeterTypeKpis = new List<MeterTypeKpi>
            {
                BuildMeterTypeKpi(MeterType.Water, CurrentEnd),
                BuildMeterTypeKpi(MeterType.Electricity, CurrentEnd)
            };

            WaterCurrent30Days = MeterTypeKpis.First(x => x.MeterType == MeterType.Water).Current30Days;
            ElectricityCurrent30Days = MeterTypeKpis.First(x => x.MeterType == MeterType.Electricity).Current30Days;

            BuildCalendarMonthComparison();

            RecentMeterReadings = MeterReadings
                .OrderByDescending(x => x.ReadingDate)
                .Take(8)
                .ToList();

            TopWaterAreas = BuildTopAreas(currentReadings, MeterType.Water);
            TopElectricityAreas = BuildTopAreas(currentReadings, MeterType.Electricity);

            BuildAlerts();
            BuildTrendChartData(currentReadings);
            BuildAreaChartData();
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


            var days = (CurrentEnd - CurrentStart).Days + 1;

            PreviousEnd = CurrentStart.AddDays(-1);
            PreviousStart = PreviousEnd.AddDays(-(days - 1));
        }

        private static bool IsInRange(DateTime date, DateTime startDate, DateTime endDate)
        {
            return date.Date >= startDate.Date && date.Date <= endDate.Date;
        }

        private IEnumerable<MeterReading> ApplyMeterFilters(IEnumerable<MeterReading> readings)
        {
            if (MeterId.HasValue) readings = readings.Where(x => x.MeterId == MeterId.Value);
            if (!string.IsNullOrWhiteSpace(Department))
                readings = readings.Where(x => string.Equals(x.DepartmentName, Department, StringComparison.OrdinalIgnoreCase));
            return readings;
        }

        private static decimal GetMeterTotal(List<DailyUsage> meterReadings, MeterType meterType)
        {
            return meterReadings
                .Where(x => x.MeterType == meterType)
                .Sum(x => x.Usage);
        }

        private static decimal GetMeterTotal(List<DailyUsage> meterReadings, MeterType meterType, DateTime startDate, DateTime endDate)
        {
            return meterReadings
                .Where(x => x.MeterType == meterType && IsInRange(x.ReadingDate, startDate, endDate))
                .Sum(x => x.Usage);
        }

        private static double CalculateGrowth(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0)
            {
                return 0;
            }

            return Math.Round((double)((currentValue - previousValue) / previousValue * 100), 1);
        }

        private MeterTypeKpi BuildMeterTypeKpi(MeterType meterType, DateTime periodEnd)
        {
            ConsumptionScope Scope(DateTime start,DateTime end) { var period=calculations.ForPeriod(start,end,MeterId,Department);return meterType==MeterType.Water?period.Water:period.Electricity; }
            var c7=Scope(periodEnd.AddDays(-6),periodEnd); var p7=Scope(periodEnd.AddDays(-13),periodEnd.AddDays(-7));
            var c30=Scope(periodEnd.AddDays(-29),periodEnd); var p30=Scope(periodEnd.AddDays(-59),periodEnd.AddDays(-30));
            var week=c7.IsComplete&&p7.IsComplete&&p7.Total>0; var month=c30.IsComplete&&p30.IsComplete&&p30.Total>0;
            return new MeterTypeKpi { MeterType=meterType,Current7Days=c7.Total,Previous7Days=p7.Total,Current30Days=c30.Total,Previous30Days=p30.Total,
                WeeklyAvailable=week,MonthlyAvailable=month,WeeklyGrowthPercent=week?CalculateGrowth(c7.Total,p7.Total):0,MonthlyGrowthPercent=month?CalculateGrowth(c30.Total,p30.Total):0 };
        }

        private void BuildCalendarMonthComparison()
        {
            var currentStart=new DateTime(CurrentEnd.Year,CurrentEnd.Month,1);
            var previousStart=currentStart.AddMonths(-1);
            var previousEnd=previousStart.AddDays(Math.Min(CurrentEnd.Day,DateTime.DaysInMonth(previousStart.Year,previousStart.Month))-1);
            var current=calculations.ForPeriod(currentStart,CurrentEnd,MeterId,Department);
            var previous=calculations.ForPeriod(previousStart,previousEnd,MeterId,Department);
            PreviousMonthLabel=$"{previousStart:dd MMM}–{previousEnd:dd MMM yyyy}";
            CurrentMonthLabel=$"{currentStart:dd MMM}–{CurrentEnd:dd MMM yyyy}";
            PreviousMonthWaterConsumption=previous.Water.Total; CurrentMonthWaterConsumption=current.Water.Total;
            PreviousMonthElectricityConsumption=previous.Electricity.Total; CurrentMonthElectricityConsumption=current.Electricity.Total;
            WaterMonthChangeAvailable=current.Water.IsComplete&&previous.Water.IsComplete&&previous.Water.Average>0;
            ElectricityMonthChangeAvailable=current.Electricity.IsComplete&&previous.Electricity.IsComplete&&previous.Electricity.Average>0;
            WaterMonthlyGrowthPercent=WaterMonthChangeAvailable?CalculateGrowth(current.Water.Average,previous.Water.Average):0;
            ElectricityMonthlyGrowthPercent=ElectricityMonthChangeAvailable?CalculateGrowth(current.Electricity.Average,previous.Electricity.Average):0;
            PreviousMonthWaterAvailable=previous.Water.CoveredDays>0; CurrentMonthWaterAvailable=current.Water.CoveredDays>0;
            PreviousMonthElectricityAvailable=previous.Electricity.CoveredDays>0; CurrentMonthElectricityAvailable=current.Electricity.CoveredDays>0;
            WaterMonthValuesJson=JsonSerializer.Serialize(new decimal?[]{PreviousMonthWaterAvailable?previous.Water.Total:null,CurrentMonthWaterAvailable?current.Water.Total:null});
            ElectricityMonthValuesJson=JsonSerializer.Serialize(new decimal?[]{PreviousMonthElectricityAvailable?previous.Electricity.Total:null,CurrentMonthElectricityAvailable?current.Electricity.Total:null});
            MonthMethodology=$"Month figures use covered estimated flow; the percentage compares daily averages over matching month-to-date exposure. Water coverage: current {current.Water.CoveredDays}/{current.Water.Dates.Count}, previous {previous.Water.CoveredDays}/{previous.Water.Dates.Count} days.";
            MonthComparisonLabelsJson=JsonSerializer.Serialize(new[]{PreviousMonthLabel,CurrentMonthLabel});
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

            if (DataCoveragePercent < 90)
            {
                Alerts.Add(new DashboardAlert
                {
                    Title = "Meter data coverage needs attention",
                    Message = $"Configured utility boundaries have complete estimated flow on {CompleteCoverageDays:N0} of {SelectedDayCount:N0} days ({DataCoveragePercent:N1}%).",
                    Type = "warning"
                });
            }

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

        private void BuildTrendChartData(List<DailyUsage> meterReadings)
        {
            TrendLabelsJson = JsonSerializer.Serialize(Consumption.Water.Dates.Select(d => d.ToString("dd MMM yyyy")));
            WaterTrendJson = JsonSerializer.Serialize(Consumption.Water.Series());
            ElectricityTrendJson = JsonSerializer.Serialize(Consumption.Electricity.Series());
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

        private static List<AreaConsumptionRow> BuildTopAreas(List<DailyUsage> meterReadings, MeterType meterType)
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
                .ToList();

            var max = rows.Any() ? rows.Max(x => x.TotalUsage) : 0;

            foreach (var row in rows)
            {
                row.ProgressPercent = max == 0
                    ? 0
                    : Convert.ToInt32(Math.Round((row.TotalUsage * 100m) / max, 0));
            }

            return rows;
        }

        private void BuildAreaChartData()
        {
            WaterAreaLabelsJson = JsonSerializer.Serialize(TopWaterAreas.Select(x => x.AreaName));
            WaterAreaDataJson = JsonSerializer.Serialize(TopWaterAreas.Select(x => x.TotalUsage));

            ElectricityAreaLabelsJson = JsonSerializer.Serialize(TopElectricityAreas.Select(x => x.AreaName));
            ElectricityAreaDataJson = JsonSerializer.Serialize(TopElectricityAreas.Select(x => x.TotalUsage));
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
            public decimal TotalUsage { get; set; }
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
