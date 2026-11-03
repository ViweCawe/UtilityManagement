using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Models
{
    public class WasteReadingUpdate
    {
        public int Id { get; set; }

        [Required]
        [Range(typeof(decimal),"0.01","9999999999999999.99",ErrorMessage="Enter a positive waste amount with at most 16 integer digits.")]
        [Display(Name = "Waste Amount")]
        public decimal WasteAmount { get; set; }

        [Required]
        public DateTime? ReadingDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}