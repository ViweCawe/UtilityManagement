using Dapper;
using DataLibrary.Db;
using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;

namespace DataLibrary.Data
{
    public class MeterReadingData : IMeterReadingData
    {
        private readonly IDataAccess dataAcces;
        private readonly ConnectionStringData connectionString;

        public MeterReadingData(IDataAccess dataAcces, ConnectionStringData connectionString)
        {
            this.dataAcces = dataAcces;
            this.connectionString = connectionString;
        }

        public async Task<int> CreateMeterReadings(MeterReading meterReading)
        {
            DynamicParameters  p= new DynamicParameters();

            p.Add("MeterId", meterReading.MeterId);
            p.Add("EmployeeId", meterReading.EmployeeId);
            p.Add("CurrentReading",meterReading.CurrentReading);
            p.Add("ReadingDate",meterReading.ReadingDate);
            p.Add("Notes",meterReading.Notes);
            p.Add("Id",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output);
            await dataAcces.SaveData("dbo.spMeterReadings_Insert",
                p,
                connectionString.SqlConnectionName);
            return p.Get<int>("Id");
        }

        public Task<int> UpdateMeterReadings(int readingId, decimal readingUpdate,string notes)
        {
            return dataAcces.SaveData("dbo.spMeterReadings_Update",
                new
                {
                   
                    Id = readingId,
                    CurrentReading = readingUpdate,
                    Notes = notes,
                    ReadingDate = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = "System"  // You can replace this with actual user info if available

                },
                connectionString.SqlConnectionName);
        }

        public Task<int> DeleteMeterReadings(int id)
        {
            return dataAcces.SaveData("dbo.spMeterReadings_Delete",
                new
                {
                    Id =id
                },
                connectionString.SqlConnectionName
                );
        }

        public async Task<MeterReading?> GetMeterReadingsById(int id)
        {
             var records = await  dataAcces.LoadData<
                MeterReading, dynamic>(
                "dbo.spMeterReadings_GetById",
                new
                {
                    Id = id
                },
                connectionString.SqlConnectionName);

            return records.FirstOrDefault();
        }


        public async Task<IEnumerable<MeterReading>> 
            GetAllMeterReading()
        {
            return await dataAcces.LoadData<
                MeterReading, 
                dynamic>("dbo.spMeterReadings_All",
                new { },
                connectionString.SqlConnectionName);
                
        }

        public async Task<MeterReading?> GetLatestReadingByMeterId(int id)
        {
            var records = await dataAcces.LoadData<
                MeterReading, dynamic>("dbo.spMeterReading_GetLatestByMeterId",
                new
                {
                    Id = id
                }, connectionString.SqlConnectionName);
            return records.FirstOrDefault();
        }

        public async Task<IEnumerable<MeterReading>> GetLatestMeterReadings()
        {
            return await dataAcces.LoadData<MeterReading, dynamic>(
                "dbo.spMeterReading_GetLatestAll",
                new { },
                connectionString.SqlConnectionName);
        }

    }
}
