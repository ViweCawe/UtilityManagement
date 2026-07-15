using Dapper;
using DataLibrary.Db;
using DataLibrary.Models;
using System.Data;

namespace DataLibrary.Data
{
    public class DailyPeopleCountData : IDailyPeopleCountData
    {
        private readonly IDataAccess dataAccess;
        private readonly ConnectionStringData connectionStringData;

        public DailyPeopleCountData(IDataAccess dataAccess, ConnectionStringData connectionStringData)
        {
            this.dataAccess = dataAccess;
            this.connectionStringData = connectionStringData;
        }

        public async Task<int> CreateDailyPeople(DailyPeopleCount dailyVisits)
        {
            DynamicParameters p = new();

            p.Add("Visitors", dailyVisits.Visitors);
            p.Add("Employees", dailyVisits.Employees);
            p.Add("Date", dailyVisits.Date);
            p.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await dataAccess.SaveData(
                "dbo.spDailyPeopleCount_Insert",
                p,
                connectionStringData.SqlConnectionName);

            return p.Get<int>("Id");
        }

        public async Task<IEnumerable<DailyPeopleCount>> GetDailyPeopleCountsByDateRange(DateTime startDate, DateTime endDate)
        {
            return await dataAccess.LoadData<DailyPeopleCount, dynamic>(
                "dbo.spDailyPeopleCount_ByDateRange",
                new
                {
                    StartDate = startDate.Date,
                    EndDate = endDate.Date
                },
                connectionStringData.SqlConnectionName);
        }
    }
}