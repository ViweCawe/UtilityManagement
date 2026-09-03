using DataLibrary.Models;
using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Models
{
    public class MeterReadingUpdate: BaseModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Current Reading")]
        public int CurrentReadingUpdate { get; set; }
        [Display(Name = "Reading Date")]
        public DateTime ReadingDateUpdate { get; set; } = new();

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}
