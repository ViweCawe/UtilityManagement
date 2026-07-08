using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Models
{
    public class MeterReadingUpdate
    {
        public int Id { get; set; }
        [Required]

        public decimal CurrentReadingUpdate { get; set; } 
        public string Notes { get; set; } = string.Empty;
        public int UpdatedBy { get; set; } 
        public DateTime ReadingDate { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

    }
}
