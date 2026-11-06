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
            var error = MeterAssetRules.Validate(meter.AssetKind, meter.NetworkRole, meter.MeterType, meter.CapacityLitres);
            if (error is not null) throw new ArgumentException(error);
            if(!MeterUnits.Valid(meter.Unit,meter.MeterType))throw new ArgumentException("Choose a valid counter unit.");
            parameters.Add("SerialNumber",meter.SerialNumber);
            parameters.Add("FlowDirection",(int)meter.FlowDirection);
            parameters.Add("ReplacesMeterId",meter.ReplacesMeterId);
            parameters.Add("InstalledOn",meter.InstalledOn);
            parameters.Add("AssetKind", (int)meter.AssetKind);
            parameters.Add("NetworkRole", (int)meter.NetworkRole);
            parameters.Add("CapacityLitres", meter.CapacityLitres);
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
            var error = MeterAssetRules.Validate(meter.AssetKind, meter.NetworkRole, meter.MeterType, meter.CapacityLitres);
            if (error is not null) throw new ArgumentException(error);
            await dataAccess.SaveData(
                "dbo.spMeters_UpdateDetails",
                new
                {
                    meter.Id,
                    AssetKind = (int)meter.AssetKind,
                    NetworkRole = (int)meter.NetworkRole,
                    meter.CapacityLitres,
                    MeterType = (int)meter.MeterType,
                    meter.MeterName,
                    meter.Unit,
                    meter.SerialNumber,
                    FlowDirection=(int)meter.FlowDirection,
                    meter.InstalledOn,
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
