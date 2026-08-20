using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Models
{
    public class WasteReadingUpdate
    {
        public int Id { get; set; }

        [Required]
        [Range(
            0.01,
            double.MaxValue,
            ErrorMessage = "Waste amount must be greater than zero.")]
        [Display(Name = "Waste Amount")]
        public decimal WasteAmount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}