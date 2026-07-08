using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Models
{
    public class WasteType :BaseModel
    {
        public int Id { get; set; }

        public string WasteTypeName { get; set; } = string.Empty;
        public int WasteCategoryId { get; set; }
        public int WasteMaterialId { get; set; }

        // UI helper (NOT stored in DB unless you want to denormalize)
        public string? CategoryName { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public IEnumerable<WasteReading> Readings { get; set; } =new  List<WasteReading>();
    }
}
