using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Models
{
    public class WasteReadingUpdate 
    {
        public int Id { get; set; }
        [Required]
        public decimal wasteReadingUpdate {  get; set; }
        public string Notes { get; set; } = string.Empty;
        public int UpdatedBy { get; set; }
        public DateTime ReadingDate { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
