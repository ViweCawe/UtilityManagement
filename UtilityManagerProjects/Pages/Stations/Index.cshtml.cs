using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Stations

{
    public class IndexModel : PageModel
    {
        private readonly IStationData stationData;

        public IndexModel(IStationData stationData)
        {
            this.stationData = stationData;
        }
        public List<Station> StationList { get; set; } 
        public async Task OnGet()
        {
            StationList = await stationData.GetStations();

        }
    }
}
