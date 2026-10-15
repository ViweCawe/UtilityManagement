using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.MeterMapping;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IMeterData meters;
    private readonly IMeterConnectionData connections;

    public IndexModel(IMeterData meters, IMeterConnectionData connections)
    {
        this.meters = meters;
        this.connections = connections;
    }

    public List<Meter> AvailableMeters { get; private set; } = new();
    public List<MeterConnection> ActiveConnections { get; private set; } = new();

    [BindProperty] public int UpstreamMeterId { get; set; }
    [BindProperty] public int DownstreamMeterId { get; set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var all = await meters.GetMeters();
        var parent = all.FirstOrDefault(x => x.Id == UpstreamMeterId);
        var child = all.FirstOrDefault(x => x.Id == DownstreamMeterId);
        if (parent is null || child is null || parent.Id == child.Id ||
            parent.MeterType != child.MeterType || !parent.IsActive || !child.IsActive)
        {
            ModelState.AddModelError("", "Select two different active meters of the same type.");
            await LoadAsync();
            return Page();
        }
        try
        {
            await connections.SaveConnection(UpstreamMeterId, DownstreamMeterId);
            TempData["SuccessMessage"] = "Meter connection saved.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDisconnectAsync(int downstreamMeterId)
    {
        await connections.Disconnect(downstreamMeterId);
        TempData["SuccessMessage"] = "Connection disconnected; history retained.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        AvailableMeters = (await meters.GetMeters()).Where(x => x.IsActive)
            .OrderBy(x => x.MeterName).ToList();
        ActiveConnections = (await connections.GetConnections())
            .Where(x => x.IsActive && x.ValidTo is null).ToList();
    }

    public string NameOf(int id) =>
        AvailableMeters.FirstOrDefault(x => x.Id == id)?.MeterName ?? $"Meter #{id}";
}
