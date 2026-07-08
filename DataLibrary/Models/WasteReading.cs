using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataLibrary.Models
{
    public class WasteReading : BaseModel
    {
        public int Id { get; set; }
        public int WasteTypeId { get; set; }
      
        // Display fields populated via JOINs
        public string WasteTypeName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty ;
        public string Unit { get; set; } = "Kg";
        public decimal WasteAmount { get; set; }
        public DateTime ReadingDate { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public bool IsDeleted { get; set; }
    }
}
