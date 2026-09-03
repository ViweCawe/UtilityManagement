using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.MeterReadings
{
    [Authorize(Roles ="Admin")]
    public class ListModel : PageModel
    {
        private readonly IMeterReadingData meterReading;

        public ListModel(IMeterReadingData meterReading)
        {
            this.meterReading = meterReading;
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

        public int WaterCount { get; set; }
        public int ElectricityCount { get; set; }
        public int TotalReadings { get; set; }

        public int TotalMeters { get; set; }

        public int WaterUsage { get; set; }
        public int ElectricityUsage { get; set; }
        public int TotalUsage { get; set; }

        public double WaterPercent { get; set; }
        public double ElectricityPercent { get; set; }
        public double TotalReadingsPercent { get; set; }

        public List<MeterReading> LatestReadings { get; set; } = new();

        public async Task OnGet()
        {

            ResolveDateRange();

            var allMeterReadings = (await meterReading.GetAllMeterReading())
                .Where(x => x.MeterType == MeterType.Water || x.MeterType == MeterType.Electricity)
                .ToList();

            var currentReadings = allMeterReadings
                .Where(x => IsInRange(x.ReadingDate, CurrentStart, CurrentEnd))
                .ToList();

            var previousReadings = allMeterReadings
                .Where(x => IsInRange(x.ReadingDate, PreviousStart, PreviousEnd))
                .ToList();

            WaterCount = currentReadings.Count(x => x.MeterType == MeterType.Water);


            ElectricityCount = currentReadings.Count(x => x.MeterType == MeterType.Electricity);
            TotalReadings = WaterCount + ElectricityCount;

            WaterUsage = currentReadings
                .Where(x => x.MeterType == MeterType.Water)
                .Sum(x => x.Usage);

            ElectricityUsage = currentReadings
                .Where(x => x.MeterType == MeterType.Electricity)
                .Sum(x => x.Usage);

            TotalUsage = WaterUsage + ElectricityUsage;

            var previousWaterCount = previousReadings.Count(x => x.MeterType == MeterType.Water);
            var previousElectricityCount = previousReadings.Count(x => x.MeterType == MeterType.Electricity);
            var previousTotalCount = previousWaterCount + previousElectricityCount;

            WaterPercent = CalculateGrowth(WaterCount, previousWaterCount);
            ElectricityPercent = CalculateGrowth(ElectricityCount, previousElectricityCount);
            TotalReadingsPercent = CalculateGrowth(TotalReadings, previousTotalCount);

            var filteredForTable = currentReadings;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var search = SearchTerm.Trim();

                filteredForTable = filteredForTable
                    .Where(x =>
                        ContainsText(x.Id.ToString(), search) ||
                        ContainsText(x.MeterId.ToString(), search) ||
                        ContainsText(x.MeterNameWithUnit, search) ||
                        ContainsText(x.MeterType.ToString(), search) ||
                        ContainsText(x.AreaName, search) ||
                        ContainsText(x.StationName, search) ||
                        ContainsText(x.DepartmentName, search) ||
                        ContainsText(x.Notes, search))
                    .ToList();
            }

            LatestReadings = filteredForTable
                .GroupBy(x => x.MeterId)
                .Select(group => group
                    .OrderByDescending(x => x.ReadingDate)
                    .ThenByDescending(x => x.Id)
                    .First())
                .OrderBy(x => x.MeterId)
                .ToList();

            TotalMeters = LatestReadings.Count;
        }

        public string GetMeterUnit(MeterType meterType)
        {
            return meterType == MeterType.Water ? "L" : "kWh";
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

        private static double CalculateGrowth(int currentValue, int previousValue)
        {
            if (previousValue == 0)
            {
                return currentValue > 0 ? 100 : 0;
            }

            return Math.Round(((currentValue - previousValue) * 100.0) / previousValue, 1);
        }
    }
}