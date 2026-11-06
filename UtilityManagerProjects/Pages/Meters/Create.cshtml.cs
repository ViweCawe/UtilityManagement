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

        public List<SelectListItem> ReplacementOptions { get; private set; } = new();
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

            if(Input.MeterType.HasValue && !MeterUnits.Valid(Input.CounterUnit,Input.MeterType.Value))ModelState.AddModelError("Input.CounterUnit","Water uses L, kL or m³; m² is area. Electricity uses kWh.");
            if(Input.ReplacesMeterId.HasValue && !ReplacementOptions.Any(o=>o.Value==Input.ReplacesMeterId.ToString()))ModelState.AddModelError("Input.ReplacesMeterId","Choose a meter without an existing successor.");
            if(Input.ReplacesMeterId.HasValue && !Input.InstalledOn.HasValue)ModelState.AddModelError("Input.InstalledOn","Record the replacement cutover date and time.");
            if(Input.InstalledOn>DateTime.Now)ModelState.AddModelError("Input.InstalledOn","Installation cannot be in the future.");
            if (Input.AssetKind.HasValue && Input.NetworkRole.HasValue && Input.MeterType.HasValue)
            {
                var error = MeterAssetRules.Validate(Input.AssetKind.Value, Input.NetworkRole.Value, Input.MeterType.Value, Input.CapacityLitres);
                if (error is not null) ModelState.AddModelError(string.Empty, error);
            }

            if (!ModelState.IsValid || Input.MeterType is null || Input.AssetKind is null || Input.NetworkRole is null)
            {
                return Page();
            }

            var meter = new Meter
            {
                CounterUnit=Input.CounterUnit,SerialNumber=Input.SerialNumber.Trim(),FlowDirection=Input.FlowDirection,
                ReplacesMeterId=Input.ReplacesMeterId,InstalledOn=Input.InstalledOn,
                AssetKind = Input.AssetKind.Value,
                NetworkRole = Input.NetworkRole.Value,
                CapacityLitres = Input.CapacityLitres,
                MeterName = Input.MeterName.Trim(),
                MeterType = Input.MeterType.Value,
                AreaId = Input.AreaId,
                DepartmentId = Input.DepartmentId,
                StationId = Input.StationId,
                IsCumulative = Input.IsCumulative
            };

            int meterId;
            try { meterId = await meterData.InsertMeter(meter); }
            catch(Exception ex) when(ex is ArgumentException || ex is Microsoft.Data.SqlClient.SqlException){ModelState.AddModelError(string.Empty,"Meter could not be saved. Check replacement boundaries, service dates, units and whether another replacement was already recorded.");return Page();}

            TempData["SuccessMessage"] =
                $"Meter {meter.MeterName} was created with ID {meterId}.";

            return RedirectToPage("./Index");
        }

        private async Task LoadOptionsAsync()
        {
            var all=await meterData.GetMeters();
            ReplacementOptions=all.Where(m=>!all.Any(n=>n.ReplacesMeterId==m.Id)).OrderBy(m=>m.MeterName).Select(m=>new SelectListItem($"#{m.Id} {m.MeterName} · {m.SerialNumber} · {(m.IsActive?"active":"inactive")}",m.Id.ToString())).ToList();
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
            [Required, StringLength(10)] public string CounterUnit { get; set; } = "L";
            [StringLength(100)] public string SerialNumber { get; set; } = "";
            [EnumDataType(typeof(MeterFlowDirection))] public MeterFlowDirection FlowDirection { get; set; } = MeterFlowDirection.Outlet;
            [Range(1,int.MaxValue)] public int? ReplacesMeterId { get; set; }
            public DateTime? InstalledOn { get; set; }
            [Required(ErrorMessage = "Enter a meter name.")]
            [StringLength(100)]
            [Display(Name = "Meter name")]
            public string MeterName { get; set; } = string.Empty;

            [Required, EnumDataType(typeof(AssetKind))]
            [Display(Name = "Asset kind")]
            public AssetKind? AssetKind { get; set; }
            [Required, EnumDataType(typeof(WaterNetworkRole))]
            [Display(Name = "Network role")]
            public WaterNetworkRole? NetworkRole { get; set; }
            [Range(typeof(decimal), "0.01", "999999999999.99")]
            [Display(Name = "Tank capacity (litres)")]
            public decimal? CapacityLitres { get; set; }


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
            public bool IsCumulative { get; set; } = true;
        }
    }
}
