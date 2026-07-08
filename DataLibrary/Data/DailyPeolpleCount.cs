using Dapper;
using DataLibrary.Db;
using DataLibrary.Models;
using System.Data;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
namespace DataLibrary.Data
{
    public class DailyPeolpleCount 
    {
        private readonly IDataAccess dataAcces;
        private readonly ConnectionStringData connectionStringData;

        public DailyPeolpleCount(IDataAccess dataAcces, ConnectionStringData connectionStringData)
        {
            this.dataAcces = dataAcces;
            this.connectionStringData = connectionStringData;
        }

        public async Task<int> CreateDailyPeople(DailyPeopleCount dailyvisits)
        {
            DynamicParameters p = new DynamicParameters();

            p.Add("Visitors", dailyvisits.Visitors);
            p.Add("Employees", dailyvisits.Employees);
            p.Add("Date", dailyvisits.Date);
            p.Add("Id",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output);
            await dataAcces.SaveData("spDailyPeopleCount_Insert", p
                , connectionStringData.SqlConnectionName);
            return p.Get<int>("Id");


        }
    }
}
