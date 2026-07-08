using DataLibrary.Db;
using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public class StationData : IStationData
    {
        private readonly IDataAccess dataAccess;
        private readonly ConnectionStringData connectionStringData;

        public StationData(IDataAccess dataAccess, ConnectionStringData connectionStringData)
        {
            this.dataAccess = dataAccess;
            this.connectionStringData = connectionStringData;
        }

      
        public Task<List<Station>> GetStations()
        {
            return dataAccess.LoadData<Station, dynamic>("dbo.spStation_All",
                new { }, 
                connectionStringData.SqlConnectionName);
        }

        public async Task InsertStation(Station station)
        {
            await dataAccess.SaveData("spStation_Insert",
                new
                {
                    station.StationName,
                    station.IsActive
                },
                connectionStringData.SqlConnectionName


                );
        }
        public async Task UpdateStation(Station station)
        {
            await dataAccess.SaveData("spStation_Update",
                new
                {
                    station.StationName,
                    station.IsActive,
                },
                connectionStringData.SqlConnectionName);

        }
    }
}
