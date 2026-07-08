using DataLibrary.Data;
using DataLibrary.Db;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using UtilityManagerProjects.Models;

namespace UtilityManagerProjects.Pages.MeterReadings
{
    public class DisplayModel : PageModel
    {
        private readonly IMeterReadingData meterReading;
        private readonly ConnectionStringData connection;
        private readonly IMeterData meterData;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public MeterReading Reading { get; set; }
        [BindProperty]
        public MeterReadingUpdate ReadingUpdate { get; set; }

        public string MeterReadingCaptured { get; set; }
        public DisplayModel(IMeterReadingData meterReading, ConnectionStringData connection, IMeterData meterData)
        {
            this.meterReading = meterReading;
            this.connection = connection;
            this.meterData = meterData;
        }

        public async Task<IActionResult> OnGet()
        {
            ViewData["HideNavbar"] = true;
            Reading = await meterReading.GetMeterReadingsById(Id);

            if (Reading != null)
            {
                // Initialize the update model so Razor can read its properties safely
                ReadingUpdate = new MeterReadingUpdate
                {
                    Id = Reading.Id,
                    CurrentReadingUpdate = Reading.CurrentReading,
                    Notes = Reading.Notes,
                    UpdatedAt = Reading.UpdatedAt,
                    UpdatedBy = Reading.EmployeeId

                };

                var meter = await meterData.GetMeters();
               
                MeterReadingCaptured = meter
                    .Where(x => x.Id == Reading.MeterId)
                    .FirstOrDefault()?.MeterName;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            await meterReading.UpdateMeterReadings(ReadingUpdate.Id, ReadingUpdate.CurrentReadingUpdate, ReadingUpdate.Notes);


            // Pass the Id, not the object
            return RedirectToPage("/MeterReadings/Display", new { Id = ReadingUpdate.Id });
        }
    }
}
