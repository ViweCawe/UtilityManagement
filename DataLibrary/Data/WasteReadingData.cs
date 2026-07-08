using Dapper;
using DataLibrary.Db;
using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Intrinsics.Arm;
using System.Text;

namespace DataLibrary.Data
{
    public class WasteReadingData : IWasteReadingData 
    {
        private readonly IDataAccess dataAccess;
        private readonly ConnectionStringData connectionString;
        private readonly IWasteTypeData wasteTypeData;

        public WasteReadingData(IDataAccess dataAccess, ConnectionStringData connectionString,IWasteTypeData wasteTypeData)
        {
            this.dataAccess = dataAccess;
            this.connectionString = connectionString;
            this.wasteTypeData = wasteTypeData;
        }
        
        public async Task<int> CreateWasteReading(WasteReading wasteDataReading)
        {

            DynamicParameters dp = new DynamicParameters();

            dp.Add("WasteTypeId", wasteDataReading.WasteTypeId);
            dp.Add("CreatedBy", wasteDataReading.CreatedBy);
            dp.Add("WasteAmount", wasteDataReading.WasteAmount);
            dp.Add("RecordedAt", wasteDataReading.CreatedAt);
            dp.Add("Notes", wasteDataReading.Notes);

     

            dp.Add("Id",
              dbType: DbType.Int32,
              direction: ParameterDirection.Output);
            await dataAccess.SaveData("dbo.spWasteReadings_Insert",
                dp,
                connectionString.SqlConnectionName);
            return dp.Get<int>("Id");
        }

        public async Task<WasteReading?> GetWasteReadingById(int id)
        {
            var records = await dataAccess.LoadData<
                WasteReading, dynamic>(
                "dbo.spWasteReadings_GetById",
                new
                {
                    Id = id
                },
                connectionString.SqlConnectionName);

            return records.FirstOrDefault();
        }

        public async Task<IEnumerable<WasteReading>> GetAllWasteReadings()
        {
            var records = await dataAccess.LoadData<WasteReading, dynamic>(
                "dbo.spWasteReadings_GetAll",
                new { },
                connectionString.SqlConnectionName);
            return records;
        }

    }
}
