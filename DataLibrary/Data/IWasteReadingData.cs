using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public interface IWasteReadingData
    {
        Task<int> CreateWasteReading(WasteReading wasteReading);
        Task<WasteReading?> GetWasteReadingById(int id);
        Task<IEnumerable<WasteReading>> GetAllWasteReadings();
    }
}
