using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.MeterReadings
{
    public class DeleteModel : PageModel
    {
        private readonly IMeterReadingData meterReading;

        [BindProperty(SupportsGet =true)]
        public int Id { get; set; }
        public MeterReading? MeterReading { get; set; }
        public DeleteModel(IMeterReadingData meterReading)
        {
            this.meterReading = meterReading;
        }
        public  async Task OnGetAsync()
        {
            ViewData["HideNavbar"] = true;
            MeterReading = meterReading.GetMeterReadingsById(Id).Result;
        }

        public async Task<IActionResult>  OnPostAsync()
        {
            await meterReading.DeleteMeterReadings(Id);
            return RedirectToPage("/MeterReadings/Create");
        }
    }
}
