using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public interface IAreaData
    {
        Task<List<Area>> GetAreas();
        Task<int> InsertAreas(Area area);
        Task<int> UpdateArea(int areadId, string areaName, string discription, int stationId, int depId);
        Task<int> DeleteArea(int areadId);
    }
}
