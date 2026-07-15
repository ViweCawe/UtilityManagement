using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.DailyPeopleCounts
{
    public class ListModel : PageModel
    {
        private readonly IDailyPeopleCountData dailyPeopleCountData;

        public ListModel(IDailyPeopleCountData dailyPeopleCountData)
        {
            this.dailyPeopleCountData = dailyPeopleCountData;
        }

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; } = "Last30";

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public DateTime CurrentStart { get; set; }
        public DateTime CurrentEnd { get; set; }

        public string DateRangeLabel =>
            $"{CurrentStart:dd MMM yyyy} - {CurrentEnd:dd MMM yyyy}";

        public List<DailyPeopleCount> PeopleCounts { get; set; } = new();

        public int TotalVisitors { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalPeople { get; set; }
        public int DaysCaptured { get; set; }

        public double AverageVisitorsPerDay { get; set; }
        public double AverageEmployeesPerDay { get; set; }
        public double AveragePeoplePerDay { get; set; }

        public async Task OnGetAsync()
        {
            ResolveDateRange();

            var records = await dailyPeopleCountData
                .GetDailyPeopleCountsByDateRange(CurrentStart, CurrentEnd);

            PeopleCounts = records
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .ToList();

            TotalVisitors = PeopleCounts.Sum(x => x.Visitors);
            TotalEmployees = PeopleCounts.Sum(x => x.Employees);
            TotalPeople = TotalVisitors + TotalEmployees;

            DaysCaptured = PeopleCounts
                .Select(x => x.Date.Date)
                .Distinct()
                .Count();

            AverageVisitorsPerDay = DaysCaptured == 0
                ? 0
                : Math.Round((double)TotalVisitors / DaysCaptured, 1);

            AverageEmployeesPerDay = DaysCaptured == 0
                ? 0
                : Math.Round((double)TotalEmployees / DaysCaptured, 1);

            AveragePeoplePerDay = DaysCaptured == 0
                ? 0
                : Math.Round((double)TotalPeople / DaysCaptured, 1);
        }

        private void ResolveDateRange()
        {
            var today = DateTime.Today;

            if (DateFilter == "Custom" && StartDate.HasValue && EndDate.HasValue)
            {
                CurrentStart = StartDate.Value.Date;
                CurrentEnd = EndDate.Value.Date;
            }
            else if (DateFilter == "Last7")
            {
                CurrentStart = today.AddDays(-6);
                CurrentEnd = today;
            }
            else
            {
                DateFilter = "Last30";
                CurrentStart = today.AddDays(-29);
                CurrentEnd = today;
            }

            if (CurrentStart > CurrentEnd)
            {
                (CurrentStart, CurrentEnd) = (CurrentEnd, CurrentStart);
            }
        }
    }
}
