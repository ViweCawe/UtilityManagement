namespace DataLibrary.Models
{
    public class Area
    {
        public int Id { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public int StationId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "System"; // You can replace this with actual user info if available
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; } = string.Empty;
        public ICollection<Meter> Meters { get; set; } = new List<Meter>();
    }
}