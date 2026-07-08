using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public interface IStationData
    {
        Task<List<Station>> GetStations();

    }
}
