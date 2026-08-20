using DataLibrary.Db;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;
using DataLibrary.Models;
using Dapper;
using System.Data;

namespace DataLibrary.Data
{
    public class MeterData : IMeterData
    {
        private readonly IDataAccess dataAccess;
        private readonly ConnectionStringData connectionString;

        public MeterData(IDataAccess dataAccess, ConnectionStringData connectionString)
        {
            this.dataAccess = dataAccess;
            this.connectionString = connectionString;
        }
        public Task<List<Models.Meter>> GetMeters()
        {
            return this.dataAccess.LoadData<Models.Meter, dynamic>("dbo.spMeters_All",
                                                       new { },
                                                       this.connectionString.SqlConnectionName);

        }
       


        public async Task<int> InsertMeter(Models.Meter meter)
        {
             
            DynamicParameters p = new DynamicParameters();

            p.Add("MeterName", meter.MeterName);
            p.Add("MeterType", (int)meter.MeterType);
            p.Add("Unit", meter.Unit);
            p.Add("AreaId", meter.AreaId);
            p.Add("DepartmentId", meter.DepartmentId);
            p.Add("StationId", meter.StationId);
            p.Add("IsCumulative", meter.IsCumulative);
            p.Add("Id",dbType: DbType.Int32,
                direction: ParameterDirection.Output);
            await dataAccess.SaveData("dbo.spMeters_Insert",
                p,
                this.connectionString.SqlConnectionName);
            return p.Get<int>("Id");
        }
     

        public  Task<int> UpdateMeter(int meterId, int meterType ,string meterName,string meterNumber )
        {
            return dataAccess.SaveData("dbo.spMeters_Update",
                new
                {
                    Id = meterId,
                    MeterType = meterType,
                    MeterName = meterName,
                    MeterNumber = meterNumber
                },
                this.connectionString.SqlConnectionName);
        }

        public  Task<int> DeleteMeter(int id)
        {
            return   dataAccess.SaveData("dbo.spMeters_Delete",
                new { Id = id },
                this.connectionString.SqlConnectionName);
        }
       
        
    }
}
