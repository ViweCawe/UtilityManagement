using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Services;
namespace UtilityManagerProjects.Pages.Admin;

public abstract class ReferenceEditorModel(ReferenceDataStore store) : PageModel
{
    protected abstract bool IsArea { get; }
    [BindProperty(SupportsGet=true)] public int? Id { get; set; }
    [BindProperty] public InputData Input { get; set; } = new();
    public List<ReferenceDataStore.Row> Locations { get; private set; } = [];
    public class InputData
    {
        [Required,StringLength(100)] public string Name { get; set; } = "";
        public int StationId { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public async Task<IActionResult> OnGetAsync()
    {
        Locations=await store.Locations();
        if(Id.HasValue) {
            var row=(IsArea?await store.Areas():Locations).SingleOrDefault(r=>r.Id==Id);
            if(row is null) return NotFound();
            Input=new(){Name=row.Name,StationId=row.StationId??0,IsActive=row.IsActive};
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        Locations=await store.Locations();
        if(Id.HasValue && !(IsArea?await store.Areas():Locations).Any(r=>r.Id==Id)) return NotFound();
        if(IsArea && !Locations.Any(r=>r.Id==Input.StationId && r.IsActive)) ModelState.AddModelError("Input.StationId","Select an active location.");
        if(!ModelState.IsValid) return Page();
        try {
            if(IsArea) await store.SaveArea(Id,Input.Name,Input.StationId,Input.IsActive);
            else await store.SaveLocation(Id,Input.Name,Input.IsActive);
        } catch(DbException ex) { ModelState.AddModelError("",ReferenceError(ex));return Page(); }
        TempData["SuccessMessage"]="Changes saved.";
        return RedirectToPage(IsArea?"/Area/AreaIndex":"/Stations/Index");
    }
    public static string ReferenceError(DbException ex)
    {
        string[] messages=["Move the assigned meters before changing this area location.","Enter an area name.","Select an active location.","That area already exists at this location.","Area not found. Reload the page.","Move or deactivate active meters before deactivating this area.","Enter a location name.","That location already exists.","Location not found. Reload the page.","Move or deactivate active areas and meters first.","This area is used by meters. Keep it for history, or deactivate it after moving active meters.","This location is used by areas or meters. Keep it for history, or deactivate it when no longer active."];
        return messages.FirstOrDefault(message=>ex.Message.Contains(message,StringComparison.Ordinal))??"Unable to change this record. Reload and try again.";
    }
}
