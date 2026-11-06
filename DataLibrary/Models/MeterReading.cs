using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class MeterReading : BaseModel
    {
        public int Id { get; set; }
        public int MeterId { get; set; }
        public int? ImportBatchId { get; set; }
        public string? ImportFileName { get; set; }
        public string SerialNumber { get; set; } = "";
        public MeterFlowDirection FlowDirection { get; set; }
        public int EmployeeId { get; set; }

        public string MeterName { get; set; } = "";
        public string AreaName { get; set; } = "";
        public string StationName { get; set; } = "";
        public string DepartmentName { get; set; } = string.Empty;
        public MeterType MeterType { get; set; }

        public string? CounterUnit { get; set; }
        public string Unit { get => MeterUnits.Normalize(CounterUnit,MeterType); set => CounterUnit=value; }
        public decimal DisplayFactor => MeterUnits.Valid(Unit,MeterType)?MeterUnits.Factor(Unit,MeterType):1m;
        public string DisplayUnit => MeterUnits.Valid(Unit,MeterType)?MeterUnits.Default(MeterType):Unit;
        public bool HasConvertedDisplay => DisplayFactor!=1m;
        public decimal DisplayCurrentReading => CurrentReading*DisplayFactor;
        public decimal DisplayPreviousReading => PreviousReading*DisplayFactor;
        public decimal DisplayUsage => Usage*DisplayFactor;
        // Physical counters remain separate even when reporting under one replacement chain.
        public int? PhysicalMeterId { get; set; }
        public MeterReading ForReporting(int pointId)
        {
            var copy=(MeterReading)MemberwiseClone();
            copy.PhysicalMeterId=PhysicalMeterId ?? MeterId; copy.MeterId=pointId;
            return copy;
        }
        public DateTime ReadingDate { get; set; } = DateTime.Now;
        [Required]
        [Range(typeof(decimal), "0", "9999999999999.999999")]
        public decimal CurrentReading { get; set; }
        public decimal PreviousReading { get; set; }
        public string MeterNameWithUnit => $"{MeterName} ({Unit})";
        public string EmployeeEmail { get; set; } = string.Empty;
        public string UpdatedByEmployeeEmail { get; set; } = string.Empty;
        public decimal Usage { get; set; }
        public DateTime? PreviousReadingDate { get; set; }
        public int? IntervalDays { get; set; }
        public bool IsBaseline { get; set; }
        public string? Notes { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;


    }
}