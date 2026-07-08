namespace DataLibrary.Models
{
    public class Station
    {
        public int Id { get; set; }
        public string StationName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public ICollection<Area> Areas { get; set; } = new List<Area>();
    }
}