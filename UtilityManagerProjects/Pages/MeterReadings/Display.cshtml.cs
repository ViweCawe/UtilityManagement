using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Pages.MeterReadings
{
    public class DisplayModel : PageModel
    {
        private readonly IMeterReadingData meterReadingData;

        public DisplayModel(IMeterReadingData meterReadingData)
        {
            this.meterReadingData = meterReadingData;
        }

        public MeterReading? Reading { get; set; }

        [BindProperty]
        public ReadingUpdateInput ReadingUpdate { get; set; } = new();

        public async Task<IActionResult> OnGet(int id)
        {
            Reading = await meterReadingData.GetMeterReadingsById(id);

            if (Reading == null)
            {
                return RedirectToPage("./List");
            }

            ReadingUpdate = new ReadingUpdateInput
            {
                Id = Reading.Id,
                CurrentReadingUpdate = Reading.CurrentReading,
                Notes = Reading.Notes
            };

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                Reading = await meterReadingData.GetMeterReadingsById(ReadingUpdate.Id);
                return Page();
            }

            await meterReadingData.UpdateMeterReadings(
                ReadingUpdate.Id,
                ReadingUpdate.CurrentReadingUpdate,
                ReadingUpdate.Notes ?? string.Empty);

            return RedirectToPage("./Display", new { id = ReadingUpdate.Id });
        }

        public string GetMeterUnit()
        {
            if (Reading == null)
            {
                return string.Empty;
            }

            return Reading.MeterType == MeterType.Water ? "L" : "kWh";
        }

        public class ReadingUpdateInput
        {
            public int Id { get; set; }

            [Required]
            [Display(Name = "Current Reading")]
            public decimal CurrentReadingUpdate { get; set; }

            [Display(Name = "Notes")]
            public string? Notes { get; set; }
        }
    }
}