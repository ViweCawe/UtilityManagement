using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Models
{
    public class BaseModel
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
