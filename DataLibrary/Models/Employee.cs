namespace DataLibrary.Models
{
    public class Employee : BaseModel
    {
        public int Id { get; set; }
        public string? Email { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
        
    }
}