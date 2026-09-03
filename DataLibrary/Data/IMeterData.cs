using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public interface IMeterData
    {
        Task<List<Meter>> GetMeters();
        Task<int> InsertMeter(Meter meter);
        Task<int> UpdateMeter(int meterId, int meterType, string meterName,string meterNumber);
        Task<int> DeleteMeter(int id);
        Task UpdateMeter(Meter meter);
        Task<Meter?> GetMeterById(int id);
    }
}
