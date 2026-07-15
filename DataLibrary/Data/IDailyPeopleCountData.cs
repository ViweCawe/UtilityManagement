using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public interface IDailyPeopleCountData
    {
        Task<int> CreateDailyPeople(DailyPeopleCount dailyVisits);

        Task<IEnumerable<DailyPeopleCount>> GetDailyPeopleCountsByDateRange(DateTime startDate, DateTime endDate);

    }
}