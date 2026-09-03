using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DataLibrary.Db;
using DataLibrary.Models;

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

        public Task<List<Meter>> GetMeters()
        {
            return dataAccess.LoadData<Meter, dynamic>(
                "dbo.spMeters_All",
                new { },
                connectionString.SqlConnectionName);
        }

        public async Task<Meter?> GetMeterById(int id)
        {
            var records = await dataAccess.LoadData<Meter, dynamic>(
                "dbo.spMeters_GetById",
                new { Id = id },
                connectionString.SqlConnectionName);

            return records.SingleOrDefault();
        }

        public async Task<int> InsertMeter(Meter meter)
        {
            var parameters = new DynamicParameters();
            parameters.Add("MeterName", meter.MeterName);
            parameters.Add("MeterType", (int)meter.MeterType);
            parameters.Add("Unit", meter.Unit);
            parameters.Add("AreaId", meter.AreaId);
            parameters.Add("DepartmentId", meter.DepartmentId);
            parameters.Add("StationId", meter.StationId);
            parameters.Add("IsCumulative", meter.IsCumulative);
            parameters.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await dataAccess.SaveData(
                "dbo.spMeters_Insert",
                parameters,
                connectionString.SqlConnectionName);

            return parameters.Get<int>("Id");
        }

        public Task<int> UpdateMeter(
            int meterId,
            int meterType,
            string meterName,
            string meterNumber)
        {
            return dataAccess.SaveData(
                "dbo.spMeters_Update",
                new
                {
                    Id = meterId,
                    MeterType = meterType,
                    MeterName = meterName,
                    MeterNumber = meterNumber
                },
                connectionString.SqlConnectionName);
        }

        public async Task UpdateMeter(Meter meter)
        {
            await dataAccess.SaveData(
                "dbo.spMeters_UpdateDetails",
                new
                {
                    meter.Id,
                    MeterType = (int)meter.MeterType,
                    meter.MeterName,
                    meter.Unit,
                    meter.AreaId,
                    meter.DepartmentId,
                    meter.StationId,
                    meter.IsCumulative,
                    meter.IsActive
                },
                connectionString.SqlConnectionName);
        }

        public Task<int> DeleteMeter(int id)
        {
            return dataAccess.SaveData(
                "dbo.spMeters_Delete",
                new { Id = id },
                connectionString.SqlConnectionName);
        }
    }
}
