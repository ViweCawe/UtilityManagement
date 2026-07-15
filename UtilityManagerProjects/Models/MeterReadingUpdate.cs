using DataLibrary.Models;
using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Models
{
    public class MeterReadingUpdate: BaseModel
    {
        public int Id { get; set; }
        [Required]

        public int CurrentReadingUpdate { get; set; } 
        public string Notes { get; set; } = string.Empty;
        public DateTime ReadingDate { get; set; } = DateTime.Now;

    }
}
