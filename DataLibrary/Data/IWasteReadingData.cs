using DataLibrary.Models;

namespace DataLibrary.Data
{
    public interface IWasteReadingData
    {
        Task<int> CreateWasteReading(
            WasteReading wasteReading);

        Task<int> UpdateWasteReadings(
            int id,
            decimal wasteAmount,
            string notes,
            int updatedByEmployeeId);

        Task<WasteReading?> GetWasteReadingById(
            int id);

        Task<IEnumerable<WasteReading>>
            GetAllWasteReadings();

        Task<IEnumerable<WasteReadingDisplay>>
            GetWasteReadingDisplay();
    }
}