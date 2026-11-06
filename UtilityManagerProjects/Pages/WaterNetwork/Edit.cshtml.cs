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
        var allMeters=await meters.GetMeters();
        var lineage=new Models.MeterLineage(allMeters);
        AvailableMeters = allMeters.Where(m => m.IsActive && m.MeterType == MeterType.Water).ToList();
        var savedLinks = await data.LoadData<IndexModel.MeterLink, object>("dbo.spWaterNetworkLinks_All", new { }, connection.SqlConnectionName);
        var links = configuration.GetSection("WaterNetwork:MeterIds").GetChildren()
            .ToDictionary(p => p.Key, p => int.TryParse(p.Value, out var id) ? (int?)id : null);
        foreach (var link in savedLinks) links[link.NodeId] = link.MeterId;
        foreach (var node in loaded.Network.Nodes)
        {
            var physicalId=links.GetValueOrDefault(node.Id);node.MeterId=physicalId.HasValue?lineage.Resolve(physicalId.Value):null;
            var assigned=AvailableMeters.SingleOrDefault(m=>m.Id==node.MeterId);
            if(assigned?.FlowDirection is MeterFlowDirection.Inlet or MeterFlowDirection.Outlet)node.CounterBoundary=assigned.FlowDirection==MeterFlowDirection.Inlet?"inlet":"outlet";
            if(node.Storage is not null){node.Storage.InletMeterIds=node.Storage.InletMeterIds.Select(lineage.Resolve).Distinct().ToList();node.Storage.OutletMeterIds=node.Storage.OutletMeterIds.Select(lineage.Resolve).Distinct().ToList();}
            node.ReportingRole ??= Models.WaterAccounting.Role(node);
            node.SupplyOrigin ??= Models.WaterAccounting.Origin(node);
            node.UsageArea=Models.WaterAccounting.Area(node);
            node.IsExternalSupply ??= Models.WaterAccounting.Role(node) is "municipal-inlet" or "hydrant" or "borehole-inlet";
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
            if(network.Nodes.Any(n=>Models.WaterAccounting.CounterBoundary(n)=="outlet" && n.MeterId.HasValue && n.Storage?.InletMeterIds.Contains(n.MeterId.Value)==true))
                throw new InvalidDataException("This counter measures an outlet. Choose an upstream transfer counter for water into the tank; its own outlet cannot also be its inlet.");
            if(network.Nodes.Any(n=>Models.WaterAccounting.CounterBoundary(n)=="inlet"&&n.MeterId.HasValue&&n.Storage?.OutletMeterIds.Contains(n.MeterId.Value)==true))throw new InvalidDataException("An incoming counter cannot also be the same tank’s outlet.");
            if(network.Nodes.Any(n=>n.MeterId.HasValue&&AvailableMeters.Any(m=>m.Id==n.MeterId&&m.FlowDirection!=MeterFlowDirection.Unconfirmed&&(m.FlowDirection==MeterFlowDirection.Inlet?"inlet":"outlet")!=Models.WaterAccounting.CounterBoundary(n))))throw new InvalidDataException("Counter direction must match the physical meter record. Check the assigned meter and measurement boundary.");
            var boundaryIds=network.Nodes.Where(n=>n.Storage is not null).SelectMany(n=>n.Storage!.InletMeterIds.Concat(n.Storage.OutletMeterIds));
            if(boundaryIds.Any(id=>!AvailableMeters.Any(m=>m.Id==id)||!network.Nodes.Any(n=>n.MeterId==id))) throw new InvalidDataException("Each storage boundary must use an active water meter linked to its actual point on the map.");
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
