using System.Collections.Generic;
using System.Threading.Tasks;
using DataLibrary.Models;

namespace DataLibrary.Data
{
    public interface IMeterConnectionData
    {
        Task<List<MeterConnection>> GetConnections();
        Task SaveConnection(int upstreamMeterId, int downstreamMeterId);
        Task Disconnect(int downstreamMeterId);
    }
}
