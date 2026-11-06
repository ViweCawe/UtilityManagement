using DataLibrary.Models;
using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Models
{
    public class MeterUpdate
    {
        public int Id { get; set; }
        [Required,StringLength(10)] public string CounterUnit { get; set; } = "L";
        [StringLength(100)] public string? SerialNumber { get; set; }
        [EnumDataType(typeof(MeterFlowDirection))] public MeterFlowDirection FlowDirection { get; set; }
        public DateTime? InstalledOn { get; set; }

        [Required(ErrorMessage = "Enter a meter name.")]
        [StringLength(100)]
        [Display(Name = "Meter name")]
        public string MeterName { get; set; } = string.Empty;

            [Required, EnumDataType(typeof(AssetKind))]
            [Display(Name = "Asset kind")]
            public AssetKind? AssetKind { get; set; }
            [Required, EnumDataType(typeof(WaterNetworkRole))]
            [Display(Name = "Network role")]
            public WaterNetworkRole? NetworkRole { get; set; }
            [Range(typeof(decimal), "0.01", "999999999999.99")]
            [Display(Name = "Tank capacity (litres)")]
            public decimal? CapacityLitres { get; set; }


        [Required(ErrorMessage = "Select a meter type.")]
        [Display(Name = "Meter type")]
        public MeterType? MeterType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select an area.")]
        public int AreaId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a department.")]
        public int DepartmentId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a station.")]
        public int StationId { get; set; }

        public bool IsCumulative { get; set; }
        public bool IsActive { get; set; }
    }
}
