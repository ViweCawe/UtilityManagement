using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DataLibrary.Models
{
    public class WasteCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

    }
}
