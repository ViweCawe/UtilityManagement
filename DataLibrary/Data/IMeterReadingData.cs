using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public interface IMeterReadingData
    {
        Task<MeterReading?> GetMeterReadingsById(int  id); 
        Task<int> CreateMeterReadings(MeterReading meterReading);
        Task<int> UpdateMeterReadings(int id ,int readingUpdate ,string notes,int updatedByEmployeeId, DateTime dateReadingUpdate);
        Task<int> DeleteMeterReadings(int id);
        Task<IEnumerable<MeterReading>> GetAllMeterReading();
        Task<MeterReading?> GetLatestReadingByMeterId(int id);
        Task<IEnumerable<MeterReading>> GetLatestMeterReadings();
        Task<IEnumerable<MeterReading>> GetMeterReadingsByMeterId(int meterId);

        Task<IEnumerable<MeterReading>>GetMeterReadingsByDateRange(
            DateTime startDate,
            DateTime endDate);
        //Task UpdateMeterReadings(int id, decimal currentReadingUpdate, int? employeeId, string v);
    }
}
