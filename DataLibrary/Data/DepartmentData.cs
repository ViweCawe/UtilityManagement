

using DataLibrary.Db;
using DataLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataLibrary.Data
{
    public class DepartmentData : IDepartmentData
    {
        private readonly IDataAccess dataAccess;
        private readonly ConnectionStringData connectionStringData;

        public DepartmentData(IDataAccess dataAccess, ConnectionStringData connectionStringData)
        {
            this.dataAccess = dataAccess;
            this.connectionStringData = connectionStringData;
        }

        public Task<List<Department>> GetDepartments()
        {
            return dataAccess.LoadData<Department, dynamic>(
                "dbo.spDepartments_All",
                new { }, connectionStringData.SqlConnectionName);
        }
    }
}