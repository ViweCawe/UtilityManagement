using DataLibrary.Data;
using DataLibrary.Models;
using DataLibrary.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Meters
{
    public class IndexModel : PageModel
    {
        private readonly IMeterData meterData;

        public IndexModel(IMeterData meterData)
        {
            this.meterData = meterData;
        }

        public int waterCount { get; set; }
        public int electricityCount { get; set; }
        public int wasteCount {  get; set; }

        public int totalCount { get; set; }
        public Meter Meters { get; set; }
        public IEnumerable<Meter> MeterList { get; set; } 
        public List<MeterTypeKpi> MeterTypeKpis { get; set; } = new List<MeterTypeKpi>();

        public async Task OnGet()
        {
            ViewData["HideNavbar"] = true;
            MeterList =await meterData.GetMeters();

            totalCount = MeterList.Count();
            waterCount = MeterList.Count( x => x.MeterType == MeterType.Water);
            electricityCount = MeterList.Count(x => x.MeterType == MeterType.Electricity);


            foreach (var type in Enum.GetValues<MeterType>())
            {
                var meterType = MeterList.Count(x => x.MeterType == type);


                MeterTypeKpis.Add(new MeterTypeKpi
                {
                    MeterType = type,
                    Current7Days = meterType
                });

            }
        }

    }
}
