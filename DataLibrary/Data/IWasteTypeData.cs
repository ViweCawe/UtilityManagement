using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public interface IWasteTypeData
    {
        Task<List<WasteType>> GetWasteTypes();
        Task<WasteReading> GetWasteRecordsById( int id);
    }
}
