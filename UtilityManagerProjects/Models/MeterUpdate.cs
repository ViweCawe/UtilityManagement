using DataLibrary.Models;
using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Models
{
    public class MeterUpdate
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Enter a meter name.")]
        [StringLength(100)]
        [Display(Name = "Meter name")]
        public string MeterName { get; set; } = string.Empty;

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
