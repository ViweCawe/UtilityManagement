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
        Task<int> UpdateMeterReadings(int id ,decimal readingUpdate ,string notes);
        Task<int> DeleteMeterReadings(int id);
        Task<IEnumerable<MeterReading>> GetAllMeterReading();
        Task<MeterReading?> GetLatestReadingByMeterId(int id);
        Task<IEnumerable<MeterReading>> GetLatestMeterReadings();
    }
}
