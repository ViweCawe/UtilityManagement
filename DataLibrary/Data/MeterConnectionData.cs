using System.Collections.Generic;
using System.Threading.Tasks;
using DataLibrary.Db;
using DataLibrary.Models;

namespace DataLibrary.Data
{
    public class MeterConnectionData : IMeterConnectionData
    {
        private readonly IDataAccess dataAccess;
        private readonly ConnectionStringData connectionString;

        public MeterConnectionData(IDataAccess dataAccess, ConnectionStringData connectionString)
        {
            this.dataAccess = dataAccess;
            this.connectionString = connectionString;
        }

        public async Task SaveConnection(int upstreamMeterId, int downstreamMeterId)
        {
            await dataAccess.SaveData("dbo.spMeterConnections_Save",
                new { UpstreamMeterId = upstreamMeterId, DownstreamMeterId = downstreamMeterId },
                connectionString.SqlConnectionName);
        }

        public async Task Disconnect(int downstreamMeterId)
        {
            await dataAccess.SaveData("dbo.spMeterConnections_Disconnect",
                new { DownstreamMeterId = downstreamMeterId },
                connectionString.SqlConnectionName);
        }

        public Task<List<MeterConnection>> GetConnections()
        {
            return dataAccess.LoadData<MeterConnection, dynamic>(
                "dbo.spMeterConnections_All",
                new { },
                connectionString.SqlConnectionName);
        }
    }
}
