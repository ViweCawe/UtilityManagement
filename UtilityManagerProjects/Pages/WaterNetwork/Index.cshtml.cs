using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataLibrary.Db;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Models;

namespace UtilityManagerProjects.Pages.WaterNetwork;

[Authorize]
public class IndexModel(IConfiguration configuration,
    IMeterData meterData, IMeterReadingData readingData, IDataAccess dataAccess, ConnectionStringData connection, UtilityManagerProjects.Services.WaterNetworkStore networkStore) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }
    public WaterFlowTraceReport FlowTrace { get; private set; } = new();
    public Models.WaterNetwork Network { get; private set; } = new();
    public Dictionary<string, Meter> LinkedMeters { get; } = new(StringComparer.Ordinal);
    public List<string> MappingWarnings { get; } = [];

    public List<Meter> AvailableMeters { get; private set; } = [];
    public Dictionary<string, object> Latest { get; } = new();
    public class MeterLink { public string NodeId { get; set; } = ""; public int? MeterId { get; set; } }
    public async Task<IActionResult> OnPostLinkAsync(string nodeId, int? meterId)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        await OnGetAsync();
        if (!Network.Nodes.Any(n => n.Id == nodeId && n.CanLinkMeter) ||
            (meterId.HasValue && !AvailableMeters.Any(m => m.Id == meterId))) return BadRequest();
        if (meterId.HasValue && LinkedMeters.Any(p => p.Key != nodeId && p.Value.Id == meterId))
        { TempData["LinkError"] = "This meter is already assigned to another point. Remove that assignment first."; return RedirectToPage(); }
        try { await dataAccess.SaveData("dbo.spWaterNetworkLinks_Save", new { NodeId = nodeId, MeterId = meterId }, connection.SqlConnectionName); }
        catch (System.Data.Common.DbException) { TempData["LinkError"] = "Could not save this link. Refresh and check that this meter is active and not assigned elsewhere."; }
        return RedirectToPage();
    }

    public string RoleLabel(string kind) => kind switch
    {
        "source" => "Source", "storage" => "Storage tank", "distribution" => "Distribution",
        "usage" => "Water use", "transfer" => "Transfer", _ => "To confirm"
    };

    public async Task OnGetAsync()
    {
        var range = Services.ConsumptionService.ReportingRange(StartDate ?? DateTime.Today.AddDays(-6), EndDate ?? DateTime.Today);
        StartDate = range.Start; EndDate = range.End; ViewData["DateNotice"] = range.Notice;
        Network = (await networkStore.LoadAsync()).Network;

        // Meter IDs must be explicitly configured after on-site verification.
        // Names, tanks and diagram coordinates are never treated as meter identities.
        var mappings = configuration.GetSection("WaterNetwork:MeterIds").GetChildren().ToList();
        AvailableMeters = (await meterData.GetMeters()).Where(m => m.MeterType == MeterType.Water && m.IsActive).ToList();
        var saved = await dataAccess.LoadData<MeterLink, dynamic>("dbo.spWaterNetworkLinks_All", new { }, connection.SqlConnectionName);
        var configured = mappings.ToDictionary(m => m.Key, m => m.Value);
        foreach (var link in saved) configured[link.NodeId] = link.MeterId?.ToString();

        var meters = AvailableMeters.ToDictionary(m => m.Id);
        var mappedIds = new HashSet<int>();
        foreach (var mapping in configured)
        {
            if (mapping.Value is null) continue;
            var node = Network.Nodes.SingleOrDefault(n => n.Id == mapping.Key);
            if (node is null || !node.CanLinkMeter || !int.TryParse(mapping.Value, out var meterId) || meterId <= 0 ||
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
        var history = (await readingData.GetAllMeterReading()).Where(r => !r.IsDeleted && r.ReadingDate.Date <= DateTime.Today).ToList();
        FlowTrace = WaterFlowTracing.Calculate(Network, LinkedMeters.ToDictionary(p=>p.Key,p=>(int?)p.Value.Id), AvailableMeters, history, range.Start, range.End);
        var flows = FlowTrace.Points.ToDictionary(p => p.Id);
        var readings = history.Where(r => r.ReadingDate.Date <= range.End).GroupBy(r => r.MeterId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ReadingDate).ThenByDescending(r => r.Id).First());
        foreach (var pair in LinkedMeters)
        {
            readings.TryGetValue(pair.Value.Id, out var reading);
            Latest[pair.Key] = new { meterId = pair.Value.Id, name = pair.Value.MeterName,
                currentReading = reading?.CurrentReading, capturedAt = reading?.ReadingDate.ToString("dd MMM yyyy"),
                flowLitres = flows[pair.Key].Flow.CoveredDays > 0 ? (decimal?)flows[pair.Key].Flow.Total : null,
                coveredDays = flows[pair.Key].Flow.CoveredDays, totalDays = flows[pair.Key].Flow.Dates.Count,
                unit = pair.Value.Unit, historyUrl = Url.Page("/MeterReadings/History", new { meterId = pair.Value.Id }) };
        }
    }
}
