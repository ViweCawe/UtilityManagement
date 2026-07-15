using System;

namespace DataLibrary.Models
{
    public class WasteReadingDisplay
    {
        public int Id { get; set; }

        public int WasteTypeId { get; set; }

        // Must match the column returned by spWasteReadings_All.
        public decimal WasteAmount { get; set; }

        public DateTime ReadingDate { get; set; }

        public int CapturedBy { get; set; }

        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }

        // These names match the stored procedure aliases.
        public string WasteCategory { get; set; } = string.Empty;

        public string WasteMaterial { get; set; } = string.Empty;

        public string WasteTypeName { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;

        // Temporary compatibility property for older pages.
        // New code should use WasteAmount.
        public decimal WasteReading => WasteAmount;

        public string WasteTypeDisplay =>
            $"{WasteCategory} - {WasteMaterial}";
    }
}