

using DataLibrary.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Areas
{
    public class IndexModel : PageModel
    {
        private readonly IAreaData areaData;
        private readonly IStationData stationData;

        public IndexModel(IAreaData areaData, IStationData stationData)
        {
            this.areaData = areaData;
            this.stationData = stationData;
        }

        public List<AreaRow> Areas { get; private set; } = new();
        public int TotalAreas => Areas.Count;
        public int TotalStations => Areas
            .Where(x => x.StationId > 0)
            .Select(x => x.StationId)
            .Distinct()
            .Count();

        public async Task OnGetAsync()
        {
            var areasTask = areaData.GetAreas();
            var stationsTask = stationData.GetStations();

            await Task.WhenAll(areasTask, stationsTask);

            var stations = (await stationsTask).ToDictionary(
                x => x.Id,
                x => x.StationName);

            Areas = (await areasTask)
                .Select(area => new AreaRow
                {
                    Id = area.Id,
                    AreaName = area.AreaName,
                    StationId = area.StationId,
                    StationName = stations.TryGetValue(area.StationId, out var stationName)
                        ? stationName
                        : "Unassigned"
                })
                .OrderBy(x => x.AreaName)
                .ToList();
        }

        public class AreaRow
        {
            public int Id { get; set; }
            public string AreaName { get; set; } = string.Empty;
            public int StationId { get; set; }
            public string StationName { get; set; } = string.Empty;
        }
    }
}