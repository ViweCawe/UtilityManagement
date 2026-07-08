namespace DataLibrary.Models
{
    public class Department : BaseModel
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public ICollection<Area> Areas { get; set; } = new List<Area>();
    }
}