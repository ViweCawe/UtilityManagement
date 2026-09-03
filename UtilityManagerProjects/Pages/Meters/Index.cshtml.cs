using System.Globalization;
using System.Reflection;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Meters
{
    public class IndexModel : PageModel
    {
        private static readonly int[] AllowedPageSizes = { 10, 25, 50 };
        private static readonly string[] TimestampPropertyNames =
        {
            "LastReadingAt", "UpdatedAt", "ModifiedAt", "DateUpdated", "UpdatedDate"
        };

        private readonly IMeterData meterData;

        public IndexModel(IMeterData meterData)
        {
            this.meterData = meterData;
        }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int WaterCount { get; private set; }
        public int ElectricityCount { get; private set; }
        public int TotalCount { get; private set; }
        public double WaterPercent { get; private set; }
        public double ElectricityPercent { get; private set; }
        public int FilteredCount { get; private set; }
        public int PageCount { get; private set; }
        public int FirstItemNumber => FilteredCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
        public int LastItemNumber => Math.Min(PageNumber * PageSize, FilteredCount);
        public List<MeterRow> MeterRows { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var meters = (await meterData.GetMeters()).ToList();

            TotalCount = meters.Count;
            WaterCount = meters.Count(x => x.MeterType == MeterType.Water);
            ElectricityCount = meters.Count(x => x.MeterType == MeterType.Electricity);
            WaterPercent = CalculatePercent(WaterCount, TotalCount);
            ElectricityPercent = CalculatePercent(ElectricityCount, TotalCount);

            PageSize = AllowedPageSizes.Contains(PageSize) ? PageSize : 10;
            PageNumber = Math.Max(PageNumber, 1);
            Search = Search?.Trim();

            IEnumerable<Meter> filteredMeters = meters;
            if (!string.IsNullOrWhiteSpace(Search))
            {
                filteredMeters = filteredMeters.Where(x =>
                    x.MeterName.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    x.Id.ToString(CultureInfo.InvariantCulture).Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    x.MeterType.ToString().Contains(Search, StringComparison.OrdinalIgnoreCase));
            }

            var orderedMeters = filteredMeters.OrderByDescending(x => x.Id).ToList();
            FilteredCount = orderedMeters.Count;
            PageCount = Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
            PageNumber = Math.Min(PageNumber, PageCount);

            MeterRows = orderedMeters
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new MeterRow
                {
                    Id = x.Id,
                    Reference = $"MTR-{x.Id:0000}",
                    MeterName = x.MeterName,
                    MeterType = x.MeterType.ToString(),
                    IsActive = x.IsActive,
                    LastUpdatedAt = GetLastUpdatedAt(x)
                })
                .ToList();
        }

        public async Task<IActionResult> OnPostDisableAsync(
            int id,
            string? search,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var meter = (await meterData.GetMeters()).SingleOrDefault(x => x.Id == id);
            if (meter is null)
            {
                return NotFound();
            }

            if (meter.IsActive)
            {
                meter.IsActive = false;
                await meterData.UpdateMeter(meter);
                TempData["SuccessMessage"] = $"Meter {meter.MeterName} was disabled.";
            }

            return RedirectToPage(new { search, pageNumber, pageSize });
        }

        public IEnumerable<int> VisiblePageNumbers()
        {
            const int radius = 2;
            var start = Math.Max(1, PageNumber - radius);
            var end = Math.Min(PageCount, PageNumber + radius);
            return Enumerable.Range(start, end - start + 1);
        }

        private static DateTimeOffset? GetLastUpdatedAt(Meter meter)
        {
            var meterType = meter.GetType();

            foreach (var propertyName in TimestampPropertyNames)
            {
                var property = meterType.GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                var value = property?.GetValue(meter);

                if (value is DateTimeOffset dateTimeOffset)
                {
                    return dateTimeOffset;
                }

                if (value is DateTime dateTime)
                {
                    return new DateTimeOffset(dateTime);
                }
            }

            return null;
        }

        private static double CalculatePercent(int value, int total)
        {
            return total == 0 ? 0 : Math.Round((value * 100.0) / total, 1);
        }

        public class MeterRow
        {
            public int Id { get; set; }
            public string Reference { get; set; } = string.Empty;
            public string MeterName { get; set; } = string.Empty;
            public string MeterType { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public DateTimeOffset? LastUpdatedAt { get; set; }
        }
    }
}
