using System;

namespace DataLibrary.Models
{
    public class MeterConnection
    {
        public int Id { get; set; }
        public int UpstreamMeterId { get; set; }
        public int DownstreamMeterId { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsActive { get; set; }
    }
}
