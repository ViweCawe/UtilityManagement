using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class MeterReading : BaseModel
    {
        public int Id { get; set; }
        public int MeterId { get; set; }
        public int EmployeeId { get; set; }

        public string MeterName { get; set; } = "";
        public string AreaName { get; set; } = "";
        public string StationName { get; set; } = "";
        public string DepartmentName { get; set; } = string.Empty;
        public MeterType MeterType { get; set; }

        public string Unit => MeterType switch
        {
            MeterType.Electricity => "kWh",
            MeterType.Water => "L³",
            _ => string.Empty
        };
        public DateTime ReadingDate { get; set; } = DateTime.Now;
        [Required]
        [Range(0, Int32.MaxValue)]
        public int CurrentReading { get; set; }
        public int PreviousReading { get; set; }
        public int Usage { get; set; }
        public string? Notes { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;


    }
}