using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Models;

namespace UtilityManagerProjects.Pages.WaterNetwork;

[Authorize]
public class IndexModel(IWebHostEnvironment environment, IConfiguration configuration,
    IMeterData meterData) : PageModel
{
    public Models.WaterNetwork Network { get; private set; } = new();
    public Dictionary<string, Meter> LinkedMeters { get; } = new(StringComparer.Ordinal);
    public List<string> MappingWarnings { get; } = [];

    public string RoleLabel(string kind) => kind switch
    {
        "source" => "Source", "storage" => "Storage tank", "distribution" => "Distribution",
        "usage" => "Water use", "transfer" => "Transfer", _ => "To confirm"
    };

    public async Task OnGetAsync()
    {
        using var stream = environment.ContentRootFileProvider
            .GetFileInfo("App_Data/water-network.json").CreateReadStream();
        Network = Models.WaterNetwork.Load(stream);

        // Meter IDs must be explicitly configured after on-site verification.
        // Names, tanks and diagram coordinates are never treated as meter identities.
        var mappings = configuration.GetSection("WaterNetwork:MeterIds").GetChildren().ToList();
        if (mappings.Count == 0) return;

        var meters = (await meterData.GetMeters()).ToDictionary(m => m.Id);
        var mappedIds = new HashSet<int>();
        foreach (var mapping in mappings)
        {
            var node = Network.Nodes.SingleOrDefault(n => n.Id == mapping.Key);
            if (node is null || !int.TryParse(mapping.Value, out var meterId) || meterId <= 0 ||
                !meters.TryGetValue(meterId, out var meter) || meter.MeterType != MeterType.Water)
            {
                MappingWarnings.Add($"Mapping '{mapping.Key}' must reference a map node and an existing water meter.");
                continue;
            }
            if (!mappedIds.Add(meterId))
            {
                MappingWarnings.Add($"Meter {meterId} is mapped more than once. Review the configuration.");
                continue;
            }
            LinkedMeters.Add(mapping.Key, meter);
        }
    }
}
