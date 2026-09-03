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
                Id = meter.Id,
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

            if (!ModelState.IsValid || Input.MeterType is null)
            {
                return Page();
            }

            meter.MeterName = Input.MeterName.Trim();
            meter.MeterType = Input.MeterType.Value;
            meter.AreaId = Input.AreaId;
            meter.DepartmentId = Input.DepartmentId;
            meter.StationId = Input.StationId;
            meter.IsCumulative = Input.IsCumulative;

            await meterData.UpdateMeter(meter);

            TempData["SuccessMessage"] = $"Meter {meter.MeterName} was updated.";
            return RedirectToPage("./Index");
        }

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
