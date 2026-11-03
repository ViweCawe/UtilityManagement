using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataLibrary.Models
{
    public class WasteReading : BaseModel
    {
        public int Id { get; set; }
        [Range(1,int.MaxValue)]
        public int WasteTypeId { get; set; }
      
        // Display fields populated via JOINs
        public string WasteTypeName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty ;
        public string Unit { get; set; } = "Kg";
        [Range(typeof(decimal),"0.01","9999999999999999.99")]
        public decimal WasteAmount { get; set; }
        public string EmployeeEmail { get; set; } = string.Empty;
        public DateTime ReadingDate { get; set; } = DateTime.Now;
        [StringLength(500)]
        public string? Notes { get; set; }
        public bool IsDeleted { get; set; }
    }
}
