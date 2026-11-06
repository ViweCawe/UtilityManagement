using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using UtilityManagerProjects.Models;

namespace UtilityManagerProjects.Pages.Meters
{
    public class EditModel : PageModel
    {
        private readonly IMeterData meterData;
        private readonly IAreaData areaData;
        private readonly IDepartmentData departmentData;
        private readonly IStationData stationData;

        public EditModel(
            IMeterData meterData,
            IAreaData areaData,
            IDepartmentData departmentData,
            IStationData stationData)
        {
            this.meterData = meterData;
            this.areaData = areaData;
            this.departmentData = departmentData;
            this.stationData = stationData;
        }

        [BindProperty]
        public MeterUpdate Input { get; set; } = new();

        public List<SelectListItem> AreaOptions { get; private set; } = new();
        public List<SelectListItem> DepartmentOptions { get; private set; } = new();
        public List<SelectListItem> StationOptions { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var meter = await FindMeterAsync(id);
            if (meter is null)
            {
                return NotFound();
            }

            Input = new MeterUpdate
            {
                Id = meter.Id,CounterUnit=meter.Unit,SerialNumber=meter.SerialNumber,FlowDirection=meter.FlowDirection,InstalledOn=meter.InstalledOn,
                AssetKind = meter.AssetKind,
                NetworkRole = meter.NetworkRole,
                CapacityLitres = meter.CapacityLitres,
                MeterName = meter.MeterName,
                MeterType = meter.MeterType,
                AreaId = meter.AreaId,
                DepartmentId = meter.DepartmentId,
                StationId = meter.StationId,
                IsCumulative = meter.IsCumulative,
                IsActive = meter.IsActive
            };

            await LoadOptionsAsync(meter);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var meter = await FindMeterAsync(id);
            if (meter is null)
            {
                return NotFound();
            }

            if (Input.Id != id)
            {
                ModelState.AddModelError(string.Empty, "The meter identifier is invalid.");
            }

            Input.IsActive = meter.IsActive;
            await LoadOptionsAsync(meter);
            ValidateSelectedOption(AreaOptions, Input.AreaId, "Input.AreaId", "area");
            ValidateSelectedOption(DepartmentOptions, Input.DepartmentId, "Input.DepartmentId", "department");
            ValidateSelectedOption(StationOptions, Input.StationId, "Input.StationId", "station");

            if (Input.MeterType is not MeterType.Electricity and not MeterType.Water)
            {
                ModelState.AddModelError(
                    "Input.MeterType",
                    "Select either an electricity or water meter.");
            }

            if(Input.MeterType.HasValue&&!MeterUnits.Valid(Input.CounterUnit,Input.MeterType.Value))ModelState.AddModelError("Input.CounterUnit","Use L, kL or m³ for water, kWh for electricity.");
            if (Input.AssetKind.HasValue && Input.NetworkRole.HasValue && Input.MeterType.HasValue)
            {
                var error = MeterAssetRules.Validate(Input.AssetKind.Value, Input.NetworkRole.Value, Input.MeterType.Value, Input.CapacityLitres);
                if (error is not null) ModelState.AddModelError(string.Empty, error);
            }

            if (!ModelState.IsValid || Input.MeterType is null || Input.AssetKind is null || Input.NetworkRole is null)
            {
                return Page();
            }

            meter.AssetKind = Input.AssetKind.Value;
            meter.NetworkRole = Input.NetworkRole.Value;
            meter.CapacityLitres = Input.CapacityLitres;
            meter.MeterName = Input.MeterName.Trim();
            meter.MeterType = Input.MeterType.Value;
            meter.AreaId = Input.AreaId;
            meter.DepartmentId = Input.DepartmentId;
            meter.StationId = Input.StationId;
            meter.IsCumulative = Input.IsCumulative;
            meter.Unit=Input.CounterUnit;meter.SerialNumber=Input.SerialNumber?.Trim() ?? "";meter.FlowDirection=Input.FlowDirection;meter.InstalledOn=Input.InstalledOn;

            try
            {
                await meterData.UpdateMeter(meter);
                var saved=await meterData.GetMeterById(id);
                if(saved is null || !MatchesSavedMeter(saved,meter))
                {
                    ModelState.AddModelError(string.Empty,"Changes could not be confirmed in the database. Reload the meter and try again. No save was confirmed.");
                    return Page();
                }
            }
            catch(Microsoft.Data.SqlClient.SqlException ex)
            {
                var message=ex.Number is 201 or 207 or 208 or 2812 or 8144
                    ? "Meter not saved. The database meter update is missing or out of date. Ask your administrator to apply the pending meter database updates."
                    : ex.Number is >=51100 and <=51199 ? "Meter not saved. "+ex.Message : "Meter not saved. The database rejected the update. Please try again or contact your administrator.";
                ModelState.AddModelError(string.Empty,message);return Page();
            }
            TempData["SuccessMessage"] = $"Saved successfully. {meter.MeterName} · direction: {meter.FlowDirection}.";
            return RedirectToPage("./Edit",new {id});
        }

        private static bool MatchesSavedMeter(Meter saved,Meter expected) =>
            saved.Id==expected.Id && saved.MeterName==expected.MeterName && saved.MeterType==expected.MeterType &&
            saved.AssetKind==expected.AssetKind && saved.NetworkRole==expected.NetworkRole && saved.CapacityLitres==expected.CapacityLitres &&
            saved.Unit==expected.Unit && saved.SerialNumber==expected.SerialNumber && saved.FlowDirection==expected.FlowDirection &&
            saved.InstalledOn==expected.InstalledOn && saved.AreaId==expected.AreaId && saved.DepartmentId==expected.DepartmentId &&
            saved.StationId==expected.StationId && saved.IsCumulative==expected.IsCumulative && saved.IsActive==expected.IsActive;

        public async Task<IActionResult> OnPostDisableAsync(int id)
        {
            var meter = await FindMeterAsync(id);
            if (meter is null)
            {
                return NotFound();
            }

            if (meter.IsActive)
            {
                meter.IsActive = false;
                await meterData.UpdateMeter(meter);
                TempData["SuccessMessage"] = $"Meter {meter.MeterName} was disabled.";
            }

            return RedirectToPage("./Index");
        }

        private async Task<Meter?> FindMeterAsync(int id)
        {
            if (id <= 0)
            {
                return null;
            }

            return await meterData.GetMeterById(id);
        }

        private async Task LoadOptionsAsync(Meter meter)
        {
            var areasTask = areaData.GetAreas();
            var departmentsTask = departmentData.GetDepartments();
            var stationsTask = stationData.GetStations();
            await Task.WhenAll(areasTask, departmentsTask, stationsTask);

            AreaOptions = (await areasTask)
                .Where(x => x.IsActive || x.Id == meter.AreaId)
                .OrderBy(x => x.AreaName)
                .Select(x => new SelectListItem(x.AreaName, x.Id.ToString()))
                .ToList();

            DepartmentOptions = (await departmentsTask)
                .Where(x => x.IsActive || x.Id == meter.DepartmentId)
                .OrderBy(x => x.DepartmentName)
                .Select(x => new SelectListItem(x.DepartmentName, x.Id.ToString()))
                .ToList();

            StationOptions = (await stationsTask)
                .Where(x => x.IsActive || x.Id == meter.StationId)
                .OrderBy(x => x.StationName)
                .Select(x => new SelectListItem(x.StationName, x.Id.ToString()))
                .ToList();
        }

        private void ValidateSelectedOption(
            IEnumerable<SelectListItem> options,
            int selectedId,
            string fieldName,
            string optionName)
        {
            if (!options.Any(x => x.Value == selectedId.ToString()))
            {
                ModelState.AddModelError(fieldName, $"Select a valid {optionName}.");
            }
        }
    }
}
