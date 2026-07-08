using DataLibrary.Db;
using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public class WasteTypeData :  IWasteTypeData
    {
        private readonly IDataAccess dataAccess;
        private readonly ConnectionStringData connectionString;

        public WasteTypeData(IDataAccess dataAccess, ConnectionStringData connectionString)
        {
            this.dataAccess = dataAccess;
            this.connectionString = connectionString;
        }

     

        public Task<List<WasteType>> GetWasteTypes()
        {
            return dataAccess.LoadData<WasteType, dynamic>("spWasteType_All",
                new { },
                    this.connectionString.SqlConnectionName);
        }

        public async Task<WasteReading> GetWasteRecordsById(int id)
        {
            var records = await dataAccess.LoadData<
                WasteReading, dynamic>("dbo.spWasteReadings_GetById",
                new
                {
                    Id = id
                }, connectionString.SqlConnectionName);
            return records.FirstOrDefault();
        }





    }
}
