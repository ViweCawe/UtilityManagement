using System.ComponentModel.DataAnnotations;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UtilityManagerProjects.Pages.MeterReadings;

public class CreateModel : PageModel
{
    private readonly IMeterReadingData meterReadingData;
    private readonly IMeterData meterData;
    private readonly UserManager<IdentityUser> userManager;
    private readonly IEmployeeData employeeData;
    private readonly ILogger<CreateModel> logger;

    public CreateModel(
        IMeterReadingData meterReadingData,
        IMeterData meterData,
        UserManager<IdentityUser> userManager,
        IEmployeeData employeeData,
        ILogger<CreateModel> logger)
    {
        this.meterReadingData = meterReadingData;
        this.meterData = meterData;
        this.userManager = userManager;
        this.employeeData = employeeData;
        this.logger = logger;
    }

    public List<SelectListItem> MeterItems { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Please select a meter.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid meter.")]
        public int? MeterId { get; set; }

        [Required(ErrorMessage = "Current reading is required.")]
        [Range(
            typeof(int),
            "0",
            "792281625",
            ErrorMessage = "Current reading cannot be negative.")]
        public int? CurrentReading { get; set; }

        [Required(ErrorMessage = "Reading date is required.")]
        public DateTime? ReadingDate { get; set; }

        [StringLength(255, ErrorMessage = "Notes cannot exceed 255 characters.")]
        public string? Notes { get; set; }
    }

    public async Task OnGetAsync()
    {
        ViewData["HideNavbar"] = true;
        Input.ReadingDate = DateTime.Now;

        await LoadMeterItemsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["HideNavbar"] = true;

        // Required whenever Page() may be returned.
        var meters = await meterData.GetMeters();

        MeterItems = meters?.Where(m => m.IsActive)
            .Select(m => new SelectListItem
            {
                Text = m.MeterName,
                Value = m.Id.ToString()
            })
            .ToList() ?? new();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var meter = meters?.FirstOrDefault(m => m.Id == Input.MeterId!.Value);

        if (meter == null || !meter.IsActive)
        {
            ModelState.AddModelError(
                "Input.MeterId",
                "Select an active meter or metered tank.");

            return Page();
        }

        var userId = userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            ModelState.AddModelError(
                string.Empty,
                "You must be signed in to create a meter reading.");

            return Page();
        }

        var employee = await employeeData.GetEmployeeByUserId(userId);

        if (employee == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Employee not found for the current user.");

            return Page();
        }
        var meterId = Input.MeterId!.Value;
        var captures = (await meterReadingData.GetMeterReadingsByMeterId(meter.Id)).Where(r=>!r.IsDeleted).ToList();
        var date=Input.ReadingDate!.Value;
        var lastReading=captures.Where(r=>r.ReadingDate<date).OrderByDescending(r=>r.ReadingDate).ThenByDescending(r=>r.Id).FirstOrDefault();
        var nextReading=captures.Where(r=>r.ReadingDate>date).OrderBy(r=>r.ReadingDate).ThenBy(r=>r.Id).FirstOrDefault();
        if (captures.Any(r=>r.ReadingDate==date) || (lastReading is not null && Input.CurrentReading<lastReading.CurrentReading) || (nextReading is not null && Input.CurrentReading>nextReading.CurrentReading))
        {
            ModelState.AddModelError("Input.CurrentReading", "Use a unique capture time and a counter between the captures immediately before and after this date.");
            return Page();
        }
        var previousReading=lastReading?.CurrentReading ?? Input.CurrentReading!.Value;

        var reading = new MeterReading
        {
            MeterId = meter.Id,
            PreviousReading = (int)previousReading,
            CurrentReading = Input.CurrentReading!.Value,
            Usage = (int)(Input.CurrentReading.Value - previousReading),
            ReadingDate = Input.ReadingDate!.Value,
            EmployeeId = employee.Id,
            Notes = Input.Notes?.Trim() ?? string.Empty,
            MeterType = meter.MeterType
        };

        try
        {
            var id = await meterReadingData.CreateMeterReadings(reading);

            if (id <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The meter reading could not be saved.");

                return Page();
            }

            return RedirectToPage("./Display", new { id });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save reading for meter {MeterId}",
                reading.MeterId);

            ModelState.AddModelError(
                string.Empty,
                "A database error occurred while saving the meter reading.");

            return Page();
        }
    }

    private async Task LoadMeterItemsAsync()
    {
        var meters = await meterData.GetMeters();

        MeterItems = meters?.Where(m => m.IsActive)
            .Select(m => new SelectListItem
            {
                Text = m.MeterName,
                Value = m.Id.ToString()
            })
            .ToList() ?? new();
    }
}