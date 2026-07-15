using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace UtilityManagerProjects.Pages.DailyPeopleCounts
{
    public class CreateModel : PageModel
    {
        private readonly IDailyPeopleCountData dailyPeopleCountData;

        public CreateModel(IDailyPeopleCountData dailyPeopleCountData)
        {
            this.dailyPeopleCountData = dailyPeopleCountData;
        }

        [BindProperty]
        public DailyPeopleInput Input { get; set; } = new();

        public void OnGet()
        {
            Input.Date = DateTime.Today;
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var dailyPeopleCount = new DailyPeopleCount
            {
                Visitors = Input.Visitors,
                Employees = Input.Employees,
                Date = Input.Date.Date
            };

            await dailyPeopleCountData.CreateDailyPeople(dailyPeopleCount);

            TempData["SuccessMessage"] = "Daily people count saved successfully.";

            return RedirectToPage("./List");
        }

        public class DailyPeopleInput
        {
            [Required]
            [Display(Name = "Visitors")]
            [Range(0, int.MaxValue, ErrorMessage = "Visitors cannot be negative.")]
            public int Visitors { get; set; }

            [Required]
            [Display(Name = "Employees")]
            [Range(0, int.MaxValue, ErrorMessage = "Employees cannot be negative.")]
            public int Employees { get; set; }

            [Required]
            [Display(Name = "Date")]
            public DateTime Date { get; set; } = DateTime.Today;
        }
    }
}