using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DataLibrary.Data;
using DataLibrary.Db;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Services;

namespace UtilityManagerProjects.Pages.WaterNetwork;

[Authorize(Roles = "Admin")]
public sealed class EditModel(WaterNetworkStore store, IMeterData meters, IDataAccess data,
    ConnectionStringData connection, IConfiguration configuration) : PageModel
{
    [BindProperty, Required, StringLength(1_000_000), Display(Name = "Network map")] public string NetworkJson { get; set; } = "";
    [BindProperty, Range(0, int.MaxValue)] public int Revision { get; set; }
    public List<Meter> AvailableMeters { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var loaded = await store.LoadAsync();
        Revision = loaded.Revision;
        AvailableMeters = (await meters.GetMeters()).Where(m => m.IsActive && m.MeterType == MeterType.Water).ToList();
        var savedLinks = await data.LoadData<IndexModel.MeterLink, object>("dbo.spWaterNetworkLinks_All", new { }, connection.SqlConnectionName);
        var links = configuration.GetSection("WaterNetwork:MeterIds").GetChildren()
            .ToDictionary(p => p.Key, p => int.TryParse(p.Value, out var id) ? (int?)id : null);
        foreach (var link in savedLinks) links[link.NodeId] = link.MeterId;
        foreach (var node in loaded.Network.Nodes)
        {
            node.MeterId = links.GetValueOrDefault(node.Id);
            node.IsExternalSupply ??= node.Id is "main-tank" or "fire-hydrant" or "borehole-in";
        }
        NetworkJson = JsonSerializer.Serialize(loaded.Network, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        AvailableMeters = (await meters.GetMeters()).Where(m => m.IsActive && m.MeterType == MeterType.Water).ToList();
        if (!ModelState.IsValid) return Page();
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(NetworkJson));
            var network = Models.WaterNetwork.Load(stream);
            if (network.Nodes.Any(n => n.MeterId.HasValue && !AvailableMeters.Any(m => m.Id == n.MeterId)))
                throw new InvalidDataException("A linked meter is unavailable. Remove its assignment or choose an active water meter.");
            await store.SaveAsync(network, Revision, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "Admin");
            TempData["NetworkSaved"] = "Network saved.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException)
        {
            ModelState.AddModelError(string.Empty, "The map could not be saved. " + (ex is InvalidDataException ? ex.Message : "Reload the editor and try again."));
        }
        catch (System.Data.Common.DbException)
        {
            ModelState.AddModelError(string.Empty, "The map could not be saved. Another user may have saved changes or a meter assignment may have changed. Reload the editor to get the latest map before trying again.");
        }
        return Page();
    }
}
