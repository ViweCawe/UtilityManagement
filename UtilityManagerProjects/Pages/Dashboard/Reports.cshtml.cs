using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace UtilityManagerProjects.Pages.Reports
{
    public class IndexModel : PageModel
    {
        private readonly IMeterReadingData meterReadingData;
        private readonly IWasteReadingData wasteReadingData;

        private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public IndexModel(IMeterReadingData meterReadingData, IWasteReadingData wasteReadingData)
        {
            this.meterReadingData = meterReadingData;
            this.wasteReadingData = wasteReadingData;
        }

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; } = "Last30";

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? MeterId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Department { get; set; }

        public DateTime CurrentStart { get; set; }
        public DateTime CurrentEnd { get; set; }
        public DateTime PreviousStart { get; set; }
        public DateTime PreviousEnd { get; set; }
        public DateTime GeneratedOn { get; set; } = DateTime.Now;
        public string DateRangeLabel => $"{CurrentStart:dd MMM yyyy} - {CurrentEnd:dd MMM yyyy}";

        public int TotalMeterReadings { get; set; }
        public int TotalWasteReadings { get; set; }
        public int TotalReadings => TotalMeterReadings + TotalWasteReadings;
        public int SelectedDayCount { get; set; }
        public int CompleteMeterDays { get; set; }

        public decimal WaterTotal { get; set; }
        public decimal ElectricityTotal { get; set; }
        public decimal WasteTotal { get; set; }
        public decimal PreviousWaterTotal { get; set; }
        public decimal PreviousElectricityTotal { get; set; }
        public decimal PreviousWasteTotal { get; set; }
        public decimal RecycledWaste { get; set; }
        public decimal PreviousRecycledWaste { get; set; }
        public decimal WasteDiversionPercent { get; set; }
        public decimal PreviousWasteDiversionPercent { get; set; }
        public decimal DataCoveragePercent { get; set; }

        public double WaterChangePercent { get; set; }
        public double ElectricityChangePercent { get; set; }
        public double WasteChangePercent { get; set; }

        public List<MeterReading> MeterReadings { get; set; } = new();
        public List<WasteReadingDisplay> WasteReadings { get; set; } = new();
        public List<ReportSummaryRow> SummaryRows { get; set; } = new();
        public List<MeterDailyAverageRow> MeterDailyAverages { get; set; } = new();
        public List<MeterReading> MeterOptions { get; set; } = new();
        public List<string> DepartmentOptions { get; set; } = new();

        public string TrendLabelsJson { get; set; } = "[]";
        public string WaterTrendJson { get; set; } = "[]";
        public string ElectricityTrendJson { get; set; } = "[]";
        public string WasteTrendJson { get; set; } = "[]";

        public async Task OnGet()
        {
            ResolveDateRange();
            await LoadReportData();
        }

        public async Task<IActionResult> OnGetExportExcel()
        {
            ResolveDateRange();
            await LoadReportData();

            var workbook = BuildExcelWorkbook();
            var fileName = $"Utility-Management-Report-{CurrentStart:yyyyMMdd}-{CurrentEnd:yyyyMMdd}.xlsx";
            return File(workbook, ExcelMimeType, fileName);
        }

        private async Task LoadReportData()
        {
            var unfilteredCurrent = (await meterReadingData.GetMeterReadingsByDateRange(CurrentStart, CurrentEnd))
                .Where(IsSupportedMeter)
                .ToList();

            MeterOptions = unfilteredCurrent.GroupBy(x => x.MeterId).Select(x => x.First())
                .OrderBy(x => x.MeterName).ToList();
            DepartmentOptions = unfilteredCurrent.Select(x => x.DepartmentName)
                .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x).ToList();

            MeterReadings = ApplyMeterFilters(unfilteredCurrent)
                .OrderByDescending(x => x.ReadingDate).ToList();

            var previousMeterReadings = ApplyMeterFilters((await meterReadingData
                .GetMeterReadingsByDateRange(PreviousStart, PreviousEnd)).Where(IsSupportedMeter)).ToList();

            var allWasteReadings = (await wasteReadingData.GetWasteReadingDisplay())
                .Where(x => x.IsDeleted == false)
                .ToList();

            WasteReadings = allWasteReadings
                .Where(x => IsInRange(x.ReadingDate, CurrentStart, CurrentEnd))
                .OrderByDescending(x => x.ReadingDate)
                .ToList();

            var previousWasteReadings = allWasteReadings
                .Where(x => IsInRange(x.ReadingDate, PreviousStart, PreviousEnd))
                .ToList();

            TotalMeterReadings = MeterReadings.Count;
            TotalWasteReadings = WasteReadings.Count;
            SelectedDayCount = (CurrentEnd.Date - CurrentStart.Date).Days + 1;

            WaterTotal = GetMeterTotal(MeterReadings, MeterType.Water);
            ElectricityTotal = GetMeterTotal(MeterReadings, MeterType.Electricity);
            WasteTotal = WasteReadings.Sum(x => x.WasteReading);

            
            PreviousWaterTotal = GetMeterTotal(previousMeterReadings, MeterType.Water);
            
            PreviousElectricityTotal = GetMeterTotal(previousMeterReadings, MeterType.Electricity);

            PreviousWasteTotal = previousWasteReadings.Sum(x => x.WasteReading);

            RecycledWaste = WasteReadings
                .Where(x => IsRecyclingCategory(x.WasteCategory))
                .Sum(x => x.WasteReading);
            
            PreviousRecycledWaste = previousWasteReadings
                .Where(x => IsRecyclingCategory(x.WasteCategory))
                .Sum(x => x.WasteReading);
            
            WasteDiversionPercent = CalculatePercent(RecycledWaste, WasteTotal);
            PreviousWasteDiversionPercent = CalculatePercent(PreviousRecycledWaste, PreviousWasteTotal);

            WaterChangePercent = CalculateGrowth(WaterTotal, PreviousWaterTotal);
            ElectricityChangePercent = CalculateGrowth(ElectricityTotal, PreviousElectricityTotal);
            WasteChangePercent = CalculateGrowth(WasteTotal, PreviousWasteTotal);

            CompleteMeterDays = MeterReadings
                .GroupBy(x => x.ReadingDate.Date)
                .Count(day => day.Any(x => x.MeterType == MeterType.Water) &&
                              day.Any(x => x.MeterType == MeterType.Electricity));

            DataCoveragePercent = SelectedDayCount == 0
                ? 0
                : Math.Round((decimal)CompleteMeterDays / SelectedDayCount * 100, 1);

            BuildSummaryRows();
            BuildMeterDailyAverages();
            BuildTrendData();
            GeneratedOn = DateTime.Now;
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

            var days = (CurrentEnd.Date - CurrentStart.Date).Days + 1;
            PreviousEnd = CurrentStart.AddDays(-1);
            PreviousStart = PreviousEnd.AddDays(-(days - 1));
        }

        private static bool IsSupportedMeter(MeterReading reading)
        {
            return reading.MeterType == MeterType.Water || reading.MeterType == MeterType.Electricity;
        }

        private IEnumerable<MeterReading> ApplyMeterFilters(IEnumerable<MeterReading> readings)
        {
            if (MeterId.HasValue)
                readings = readings.Where(x => x.MeterId == MeterId.Value);

            if (!string.IsNullOrWhiteSpace(Department))
                readings = readings.Where(x => string.Equals(x.DepartmentName, Department,
                    StringComparison.OrdinalIgnoreCase));

            return readings;
        }

        private void BuildMeterDailyAverages()
        {
            MeterDailyAverages = MeterReadings.GroupBy(x => new { x.MeterId, x.MeterName, x.MeterType, x.DepartmentName })
                .Select(group => new MeterDailyAverageRow
                {
                    MeterId = group.Key.MeterId,
                    MeterName = group.Key.MeterName,
                    MeterType = group.Key.MeterType,
                    DepartmentName = group.Key.DepartmentName,
                    ReadingCount = group.Count(),
                    TotalUsage = group.Sum(x => x.Usage),
                    DailyAverage = SelectedDayCount == 0 ? 0 : Math.Round((decimal)group.Sum(x => x.Usage) / SelectedDayCount, 1)
                }).OrderBy(x => x.DepartmentName).ThenBy(x => x.MeterName).ToList();
        }

        private static bool IsInRange(DateTime date, DateTime startDate, DateTime endDate)
        {
            return date.Date >= startDate.Date && date.Date <= endDate.Date;
        }

        private static bool IsRecyclingCategory(string? category)
        {
            return !string.IsNullOrWhiteSpace(category) &&
                   category.Contains("recycl", StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetMeterTotal(IEnumerable<MeterReading> readings, MeterType meterType)
        {
            return readings.Where(x => x.MeterType == meterType).Sum(x => x.Usage);
        }

        private static decimal CalculatePercent(decimal numerator, decimal denominator)
        {
            return denominator == 0 ? 0 : Math.Round(numerator / denominator * 100, 1);
        }

        private static double CalculateGrowth(decimal currentValue, decimal previousValue)
        {
            if (previousValue == 0)
            {
                return currentValue > 0 ? 100 : 0;
            }

            return Math.Round((double)((currentValue - previousValue) / previousValue * 100), 1);
        }

        private void BuildSummaryRows()
        {
            var waterPeak = GetPeakMeterDay(MeterReadings, MeterType.Water);
            var electricityPeak = GetPeakMeterDay(MeterReadings, MeterType.Electricity);
            var wastePeak = GetPeakWasteDay(WasteReadings);

            SummaryRows = new List<ReportSummaryRow>
            {
                new()
                {
                    ReportArea = "Water",
                    RecordCount = MeterReadings.Count(x => x.MeterType == MeterType.Water),
                    Total = WaterTotal,
                    Unit = "L",
                    DailyAverage = SelectedDayCount == 0 ? 0 : Math.Round(WaterTotal / SelectedDayCount, 1),
                    PeakValue = waterPeak.Value,
                    PeakDate = waterPeak.Date,
                    ChangePercent = WaterChangePercent,
                    ManagementNote = BuildManagementNote(WaterChangePercent, "consumption")
                },
                new()
                {
                    ReportArea = "Electricity",
                    RecordCount = MeterReadings.Count(x => x.MeterType == MeterType.Electricity),
                    Total = ElectricityTotal,
                    Unit = "kWh",
                    DailyAverage = SelectedDayCount == 0 ? 0 : Math.Round(ElectricityTotal / SelectedDayCount, 1),
                    PeakValue = electricityPeak.Value,
                    PeakDate = electricityPeak.Date,
                    ChangePercent = ElectricityChangePercent,
                    ManagementNote = BuildManagementNote(ElectricityChangePercent, "consumption")
                },
                new()
                {
                    ReportArea = "Waste",
                    RecordCount = WasteReadings.Count,
                    Total = WasteTotal,
                    Unit = "kg",
                    DailyAverage = SelectedDayCount == 0 ? 0 : Math.Round(WasteTotal / SelectedDayCount, 1),
                    PeakValue = wastePeak.Value,
                    PeakDate = wastePeak.Date,
                    ChangePercent = WasteChangePercent,
                    ManagementNote = $"{WasteDiversionPercent:N1}% diverted from landfill."
                }
            };
        }

        private static string BuildManagementNote(double changePercent, string metricName)
        {
            if (changePercent >= 15)
            {
                return $"Investigate the {changePercent:N1}% increase in {metricName}.";
            }

            if (changePercent <= -5)
            {
                return $"Improved by {Math.Abs(changePercent):N1}% versus the previous period.";
            }

            return "Stable against the previous matching period.";
        }

        private static PeakDay GetPeakMeterDay(IEnumerable<MeterReading> readings, MeterType meterType)
        {
            var peak = readings
                .Where(x => x.MeterType == meterType)
                .GroupBy(x => x.ReadingDate.Date)
                .Select(group => new PeakDay { Date = group.Key, Value = group.Sum(x => (decimal)x.Usage) })
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            return peak ?? new PeakDay();
        }

        private static PeakDay GetPeakWasteDay(IEnumerable<WasteReadingDisplay> readings)
        {
            var peak = readings
                .GroupBy(x => x.ReadingDate.Date)
                .Select(group => new PeakDay { Date = group.Key, Value = group.Sum(x => x.WasteReading) })
                .OrderByDescending(x => x.Value)
                .FirstOrDefault();

            return peak ?? new PeakDay();
        }

        private void BuildTrendData()
        {
            var buckets = BuildTrendBuckets(CurrentStart, CurrentEnd);
            TrendLabelsJson = JsonSerializer.Serialize(buckets.Select(x => x.Label));

            WaterTrendJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                MeterReadings
                    .Where(x => x.MeterType == MeterType.Water &&
                                IsInRange(x.ReadingDate, bucket.Start, bucket.End))
                    .Sum(x => x.Usage)));

            ElectricityTrendJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                MeterReadings
                    .Where(x => x.MeterType == MeterType.Electricity &&
                                IsInRange(x.ReadingDate, bucket.Start, bucket.End))
                    .Sum(x => x.Usage)));

            WasteTrendJson = JsonSerializer.Serialize(buckets.Select(bucket =>
                WasteReadings
                    .Where(x => IsInRange(x.ReadingDate, bucket.Start, bucket.End))
                    .Sum(x => x.WasteReading)));
        }

        private static List<TrendBucket> BuildTrendBuckets(DateTime startDate, DateTime endDate)
        {
            var days = (endDate.Date - startDate.Date).Days + 1;
            var buckets = new List<TrendBucket>();

            if (days <= 90)
            {
                for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
                {
                    buckets.Add(new TrendBucket { Start = day, End = day, Label = day.ToString("dd MMM") });
                }

                return buckets;
            }

            if (days <= 365)
            {
                var weekStart = startDate.Date;

                while (weekStart <= endDate.Date)
                {
                    var weekEnd = weekStart.AddDays(6) > endDate.Date
                        ? endDate.Date
                        : weekStart.AddDays(6);

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
                    Start = new DateTime(year, 1, 1) < startDate.Date ? startDate.Date : new DateTime(year, 1, 1),
                    End = new DateTime(year, 12, 31) > endDate.Date ? endDate.Date : new DateTime(year, 12, 31),
                    Label = year.ToString()
                });
            }

            return buckets;
        }

        private byte[] BuildExcelWorkbook()
        {
            using var stream = new MemoryStream();

            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                WriteTextEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
                WriteTextEntry(archive, "_rels/.rels", BuildPackageRelationshipsXml());
                WriteTextEntry(archive, "xl/workbook.xml", BuildWorkbookXml());
                WriteTextEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsXml());
                WriteTextEntry(archive, "xl/styles.xml", BuildStylesXml());

                WriteDocumentEntry(
                    archive,
                    "xl/worksheets/sheet1.xml",
                    BuildWorksheetXml(BuildExecutiveSummaryCells(), new[] { 24d, 18d, 14d, 18d, 15d, 46d }, null, null,
                        new[] { "A1:F1", "A2:F2", "A4:F4", "A12:F12" }));

                var meterRows = BuildMeterReadingCells();
                WriteDocumentEntry(
                    archive,
                    "xl/worksheets/sheet2.xml",
                    BuildWorksheetXml(meterRows, new[] { 15d, 18d, 28d, 16d, 12d }, 1, $"A1:E{meterRows.Count}", null));

                var wasteRows = BuildWasteReadingCells();
                WriteDocumentEntry(
                    archive,
                    "xl/worksheets/sheet3.xml",
                    BuildWorksheetXml(wasteRows, new[] { 15d, 24d, 26d, 28d, 16d }, 1, $"A1:E{wasteRows.Count}", null));
            }

            return stream.ToArray();
        }

        private List<List<ExcelCell>> BuildExecutiveSummaryCells()
        {
            var rows = new List<List<ExcelCell>>
            {
                Row(TextCell("UTILITY MANAGEMENT REPORT", 1), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell()),
                Row(TextCell($"Reporting period: {DateRangeLabel} | Generated: {GeneratedOn:dd MMM yyyy HH:mm}", 2), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell()),
                Row(EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell()),
                Row(TextCell("EXECUTIVE KPIs", 8), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell()),
                Row(TextCell("Metric", 3), TextCell("Current Period", 3), TextCell("Unit", 3), TextCell("Previous Period", 3), TextCell("Change", 3), TextCell("Management Interpretation", 3)),
                Row(TextCell("Water Consumption", 4), NumberCell(WaterTotal, 5), TextCell("L", 4), NumberCell(PreviousWaterTotal, 5), PercentCell((decimal)WaterChangePercent / 100), TextCell(BuildManagementNote(WaterChangePercent, "consumption"), 4)),
                Row(TextCell("Electricity Consumption", 4), NumberCell(ElectricityTotal, 5), TextCell("kWh", 4), NumberCell(PreviousElectricityTotal, 5), PercentCell((decimal)ElectricityChangePercent / 100), TextCell(BuildManagementNote(ElectricityChangePercent, "consumption"), 4)),
                Row(TextCell("Waste Generated", 4), NumberCell(WasteTotal, 5), TextCell("kg", 4), NumberCell(PreviousWasteTotal, 5), PercentCell((decimal)WasteChangePercent / 100), TextCell(BuildManagementNote(WasteChangePercent, "generation"), 4)),
                Row(TextCell("Waste Diversion", 4), PercentCell(WasteDiversionPercent / 100), TextCell("%", 4), PercentCell(PreviousWasteDiversionPercent / 100), PercentCell((WasteDiversionPercent - PreviousWasteDiversionPercent) / 100), TextCell("Dry and wet recycling as a share of total recorded waste.", 4)),
                Row(TextCell("Meter Data Coverage", 4), PercentCell(DataCoveragePercent / 100), TextCell("%", 4), EmptyCell(4), EmptyCell(4), TextCell($"Both meter types captured on {CompleteMeterDays} of {SelectedDayCount} days.", 4)),
                Row(EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell()),
                Row(TextCell("OPERATIONAL SUMMARY", 8), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell(), EmptyCell()),
                Row(TextCell("Dataset", 3), TextCell("Records", 3), TextCell("Daily Average", 3), TextCell("Peak Value", 3), TextCell("Peak Date", 3), TextCell("Unit", 3))
            };

            rows.AddRange(SummaryRows.Select(row => Row(
                TextCell(row.ReportArea, 4),
                NumberCell(row.RecordCount, 5),
                NumberCell(row.DailyAverage, 5),
                NumberCell(row.PeakValue, 5),
                row.PeakDate.HasValue ? DateCell(row.PeakDate.Value) : EmptyCell(6),
                TextCell(row.Unit, 4))));

            return rows;
        }

        private List<List<ExcelCell>> BuildMeterReadingCells()
        {
            var rows = new List<List<ExcelCell>>
            {
                Row(TextCell("Reading Date", 3), TextCell("Meter Type", 3), TextCell("Area", 3), TextCell("Consumption", 3), TextCell("Unit", 3))
            };

            rows.AddRange(MeterReadings
                .OrderBy(x => x.ReadingDate)
                .ThenBy(x => x.MeterType)
                .Select(reading => Row(
                    DateCell(reading.ReadingDate),
                    TextCell(reading.MeterType.ToString(), 4),
                    TextCell(string.IsNullOrWhiteSpace(reading.AreaName) ? "Unassigned area" : reading.AreaName, 4),
                    NumberCell(reading.Usage, 5),
                    TextCell(reading.Unit, 4))));

            return rows;
        }

        private List<List<ExcelCell>> BuildWasteReadingCells()
        {
            var rows = new List<List<ExcelCell>>
            {
                Row(TextCell("Reading Date", 3), TextCell("Category", 3), TextCell("Waste Type", 3), TextCell("Material", 3), TextCell("Amount (kg)", 3))
            };

            rows.AddRange(WasteReadings
                .OrderBy(x => x.ReadingDate)
                .Select(reading => Row(
                    DateCell(reading.ReadingDate),
                    TextCell(DisplayValue(reading.WasteCategory), 4),
                    TextCell(DisplayValue(reading.WasteTypeName), 4),
                    TextCell(DisplayValue(reading.WasteMaterial), 4),
                    NumberCell(reading.WasteReading, 5))));

            return rows;
        }

        private static string DisplayValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unspecified" : value.Trim();
        }

        private static XDocument BuildWorksheetXml(
            IReadOnlyList<List<ExcelCell>> rows,
            IReadOnlyList<double> widths,
            int? freezeRows,
            string? autoFilterReference,
            IReadOnlyList<string>? mergedCells)
        {
            XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            var sheetData = new XElement(spreadsheet + "sheetData");

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var rowNumber = rowIndex + 1;
                var rowElement = new XElement(spreadsheet + "row", new XAttribute("r", rowNumber));

                if (rowNumber == 1)
                {
                    rowElement.Add(new XAttribute("ht", 25), new XAttribute("customHeight", 1));
                }

                for (var columnIndex = 0; columnIndex < rows[rowIndex].Count; columnIndex++)
                {
                    rowElement.Add(BuildCellXml(rows[rowIndex][columnIndex], GetColumnName(columnIndex + 1) + rowNumber, spreadsheet));
                }

                sheetData.Add(rowElement);
            }

            var worksheet = new XElement(spreadsheet + "worksheet");
            worksheet.Add(new XElement(spreadsheet + "dimension", new XAttribute("ref", $"A1:{GetColumnName(widths.Count)}{Math.Max(rows.Count, 1)}")));

            var sheetView = new XElement(spreadsheet + "sheetView",
                new XAttribute("workbookViewId", 0),
                new XAttribute("showGridLines", 0));

            if (freezeRows.HasValue)
            {
                sheetView.Add(new XElement(spreadsheet + "pane",
                    new XAttribute("ySplit", freezeRows.Value),
                    new XAttribute("topLeftCell", $"A{freezeRows.Value + 1}"),
                    new XAttribute("activePane", "bottomLeft"),
                    new XAttribute("state", "frozen")));
            }

            worksheet.Add(new XElement(spreadsheet + "sheetViews", sheetView));

            worksheet.Add(new XElement(spreadsheet + "cols",
                widths.Select((width, index) => new XElement(spreadsheet + "col",
                    new XAttribute("min", index + 1),
                    new XAttribute("max", index + 1),
                    new XAttribute("width", width),
                    new XAttribute("customWidth", 1)))));

            worksheet.Add(sheetData);

            if (!string.IsNullOrWhiteSpace(autoFilterReference))
            {
                worksheet.Add(new XElement(spreadsheet + "autoFilter", new XAttribute("ref", autoFilterReference)));
            }

            if (mergedCells != null && mergedCells.Count > 0)
            {
                worksheet.Add(new XElement(spreadsheet + "mergeCells",
                    new XAttribute("count", mergedCells.Count),
                    mergedCells.Select(reference => new XElement(spreadsheet + "mergeCell", new XAttribute("ref", reference)))));
            }

            worksheet.Add(new XElement(spreadsheet + "pageMargins",
                new XAttribute("left", .3),
                new XAttribute("right", .3),
                new XAttribute("top", .5),
                new XAttribute("bottom", .5),
                new XAttribute("header", .2),
                new XAttribute("footer", .2)));

            return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), worksheet);
        }

        private static XElement BuildCellXml(ExcelCell cell, string reference, XNamespace spreadsheet)
        {
            var element = new XElement(spreadsheet + "c",
                new XAttribute("r", reference),
                new XAttribute("s", cell.StyleIndex));

            if (cell.Kind == "number")
            {
                element.Add(new XAttribute("t", "n"));
                element.Add(new XElement(spreadsheet + "v", cell.NumberValue.ToString(CultureInfo.InvariantCulture)));
            }
            else if (cell.Kind == "date")
            {
                element.Add(new XAttribute("t", "n"));
                element.Add(new XElement(spreadsheet + "v", cell.DateValue.ToOADate().ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                element.Add(new XAttribute("t", "inlineStr"));
                element.Add(new XElement(spreadsheet + "is",
                    new XElement(spreadsheet + "t",
                        new XAttribute(XNamespace.Xml + "space", "preserve"),
                        cell.TextValue ?? string.Empty)));
            }

            return element;
        }

        private static string GetColumnName(int columnNumber)
        {
            var name = string.Empty;

            while (columnNumber > 0)
            {
                columnNumber--;
                name = (char)('A' + columnNumber % 26) + name;
                columnNumber /= 26;
            }

            return name;
        }

        private static List<ExcelCell> Row(params ExcelCell[] cells) => cells.ToList();
        private static ExcelCell TextCell(string value, int styleIndex) => new() { Kind = "text", TextValue = value, StyleIndex = styleIndex };
        private static ExcelCell NumberCell(decimal value, int styleIndex) => new() { Kind = "number", NumberValue = value, StyleIndex = styleIndex };
        private static ExcelCell PercentCell(decimal value) => NumberCell(value, 7);
        private static ExcelCell DateCell(DateTime value) => new() { Kind = "date", DateValue = value, StyleIndex = 6 };
        private static ExcelCell EmptyCell(int styleIndex = 0) => TextCell(string.Empty, styleIndex);

        private static void WriteTextEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content);
        }

        private static void WriteDocumentEntry(ZipArchive archive, string path, XDocument document)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            document.Save(writer);
        }

        private static string BuildContentTypesXml() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/worksheets/sheet2.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/worksheets/sheet3.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
</Types>";

        private static string BuildPackageRelationshipsXml() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>";

        private static string BuildWorkbookXml() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Executive Summary"" sheetId=""1"" r:id=""rId1""/>
    <sheet name=""Meter Readings"" sheetId=""2"" r:id=""rId2""/>
    <sheet name=""Waste Readings"" sheetId=""3"" r:id=""rId3""/>
  </sheets>
  <calcPr calcId=""191029"" fullCalcOnLoad=""1""/>
</workbook>";

        private static string BuildWorkbookRelationshipsXml() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet2.xml""/>
  <Relationship Id=""rId3"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet3.xml""/>
  <Relationship Id=""rId4"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>";

        private static string BuildStylesXml() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <numFmts count=""3""><numFmt numFmtId=""164"" formatCode=""yyyy-mm-dd""/><numFmt numFmtId=""165"" formatCode=""0.0%""/><numFmt numFmtId=""166"" formatCode=""#,##0.00""/></numFmts>
  <fonts count=""4""><font><sz val=""11""/><color theme=""1""/><name val=""Calibri""/><family val=""2""/></font><font><b/><sz val=""16""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font><font><i/><sz val=""10""/><color rgb=""FF66758C""/><name val=""Calibri""/></font><font><b/><sz val=""11""/><color rgb=""FF172033""/><name val=""Calibri""/></font></fonts>
  <fills count=""5""><fill><patternFill patternType=""none""/></fill><fill><patternFill patternType=""gray125""/></fill><fill><patternFill patternType=""solid""><fgColor rgb=""FF172B4D""/><bgColor indexed=""64""/></patternFill></fill><fill><patternFill patternType=""solid""><fgColor rgb=""FF1976D2""/><bgColor indexed=""64""/></patternFill></fill><fill><patternFill patternType=""solid""><fgColor rgb=""FFEAF3FC""/><bgColor indexed=""64""/></patternFill></fill></fills>
  <borders count=""2""><border><left/><right/><top/><bottom/><diagonal/></border><border><left style=""thin""><color rgb=""FFDCE3ED""/></left><right style=""thin""><color rgb=""FFDCE3ED""/></right><top style=""thin""><color rgb=""FFDCE3ED""/></top><bottom style=""thin""><color rgb=""FFDCE3ED""/></bottom><diagonal/></border></borders>
  <cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>
  <cellXfs count=""9""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/><xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""0"" xfId=""0"" applyFill=""1"" applyFont=""1""><alignment vertical=""center""/></xf><xf numFmtId=""0"" fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/><xf numFmtId=""0"" fontId=""1"" fillId=""3"" borderId=""1"" xfId=""0"" applyFill=""1"" applyFont=""1"" applyBorder=""1""><alignment vertical=""center""/></xf><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""><alignment vertical=""center""/></xf><xf numFmtId=""166"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1""><alignment horizontal=""right""/></xf><xf numFmtId=""164"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1""/><xf numFmtId=""165"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1""><alignment horizontal=""right""/></xf><xf numFmtId=""0"" fontId=""3"" fillId=""4"" borderId=""0"" xfId=""0"" applyFill=""1"" applyFont=""1""/></cellXfs>
  <cellStyles count=""1""><cellStyle name=""Normal"" xfId=""0"" builtinId=""0""/></cellStyles>
</styleSheet>";

        public class ReportSummaryRow
        {
            public string ReportArea { get; set; } = string.Empty;
            public int RecordCount { get; set; }
            public decimal Total { get; set; }
            public string Unit { get; set; } = string.Empty;
            public decimal DailyAverage { get; set; }
            public decimal PeakValue { get; set; }
            public DateTime? PeakDate { get; set; }
            public double ChangePercent { get; set; }
            public string ManagementNote { get; set; } = string.Empty;
        }

        public class MeterDailyAverageRow
        {
            public int MeterId { get; set; }
            public string MeterName { get; set; } = string.Empty;
            public MeterType MeterType { get; set; }
            public string DepartmentName { get; set; } = string.Empty;
            public int ReadingCount { get; set; }
            public int TotalUsage { get; set; }
            public decimal DailyAverage { get; set; }
            public string Unit => MeterType == MeterType.Water ? "L" : "kWh";
        }

        private class PeakDay
        {
            public DateTime? Date { get; set; }
            public decimal Value { get; set; }
        }

        private class TrendBucket
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Label { get; set; } = string.Empty;
        }

        private class ExcelCell
        {
            public string Kind { get; set; } = "text";
            public string? TextValue { get; set; }
            public decimal NumberValue { get; set; }
            public DateTime DateValue { get; set; }
            public int StyleIndex { get; set; }
        }
    }
}
