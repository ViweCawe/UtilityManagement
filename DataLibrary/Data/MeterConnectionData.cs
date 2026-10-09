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

        public Task<List<MeterConnection>> GetConnections()
        {
            return dataAccess.LoadData<MeterConnection, dynamic>(
                "dbo.spMeterConnections_All",
                new { },
                connectionString.SqlConnectionName);
        }
    }
}
