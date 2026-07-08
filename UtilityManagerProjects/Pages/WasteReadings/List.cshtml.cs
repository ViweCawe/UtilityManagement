using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.WasteReadings
{
    public class ListModel : PageModel
    {
        private readonly IWasteReadingData wasteReading;

        public ListModel(IWasteReadingData wasteReading)
        {
            this.wasteReading = wasteReading;
        }

        public int TotalReadings { get; set; }
        public int WaterCount { get; set; }
        public int ElectricityCount { get; set; }
        public int RefuseCount { get; set; }

        public double WaterPercent { get; set; }
        public double ElectricityPercent { get; set; }
        public double RefusePercent { get; set; }
        public Meter Meter { get; set; }
        public IEnumerable<MeterReading> MeterReadings { get; set; }
        public List<MeterTypeKpi> MeterTypeKpis { get; set; } = new List<MeterTypeKpi>();
        public void OnGet()
        {
        }
    }
}
