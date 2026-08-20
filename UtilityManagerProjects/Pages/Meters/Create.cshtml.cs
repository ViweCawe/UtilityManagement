using System.ComponentModel.DataAnnotations;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UtilityManagerProjects.Pages.Meters
{
    public class CreateModel : PageModel
    {
        private readonly IMeterData meterData;
        private readonly IAreaData areaData;
        private readonly IDepartmentData departmentData;
        private readonly IStationData stationData;

        public CreateModel(
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
        public MeterInput Input { get; set; } = new();

        public List<SelectListItem> AreaOptions { get; private set; } = new();
        public List<SelectListItem> DepartmentOptions { get; private set; } = new();
        public List<SelectListItem> StationOptions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            await LoadOptionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadOptionsAsync();

            ValidateSelectedOption(AreaOptions, Input.AreaId, "Input.AreaId", "area");
            ValidateSelectedOption(
                DepartmentOptions,
                Input.DepartmentId,
                "Input.DepartmentId",
                "department");
            ValidateSelectedOption(
                StationOptions,
                Input.StationId,
                "Input.StationId",
                "station");

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

            var meter = new Meter
            {
                MeterName = Input.MeterName.Trim(),
                MeterType = Input.MeterType.Value,
                AreaId = Input.AreaId,
                DepartmentId = Input.DepartmentId,
                StationId = Input.StationId,
                IsCumulative = Input.IsCumulative
            };

            var meterId = await meterData.InsertMeter(meter);

            TempData["SuccessMessage"] =
                $"Meter {meter.MeterName} was created with ID {meterId}.";

            return RedirectToPage("./Index");
        }

        private async Task LoadOptionsAsync()
        {
            var areasTask = areaData.GetAreas();
            var departmentsTask = departmentData.GetDepartments();
            var stationsTask = stationData.GetStations();

            await Task.WhenAll(areasTask, departmentsTask, stationsTask);

            AreaOptions = (await areasTask)
                .Where(x => x.IsActive)
                .OrderBy(x => x.AreaName)
                .Select(x => new SelectListItem(x.AreaName, x.Id.ToString()))
                .ToList();

            DepartmentOptions = (await departmentsTask)
                .Where(x => x.IsActive)
                .OrderBy(x => x.DepartmentName)
                .Select(x => new SelectListItem(x.DepartmentName, x.Id.ToString()))
                .ToList();

            StationOptions = (await stationsTask)
                .Where(x => x.IsActive)
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

        public class MeterInput
        {
            [Required(ErrorMessage = "Enter a meter name.")]
            [StringLength(100)]
            [Display(Name = "Meter name")]
            public string MeterName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Select a meter type.")]
            [Display(Name = "Meter type")]
            public MeterType? MeterType { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Select an area.")]
            [Display(Name = "Area")]
            public int AreaId { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Select a department.")]
            [Display(Name = "Department")]
            public int DepartmentId { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Select a station.")]
            [Display(Name = "Station")]
            public int StationId { get; set; }

            [Display(Name = "Cumulative meter")]
            public bool IsCumulative { get; set; }
        }
    }
}
