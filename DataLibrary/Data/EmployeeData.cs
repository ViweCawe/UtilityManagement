using DataLibrary.Db;
using DataLibrary.Models;
using System.Data;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public class EmployeeData : IEmployeeData
    {
        private readonly IDataAccess _dataAccess;
        private readonly ConnectionStringData _connection;

        public EmployeeData(IDataAccess dataAccess, ConnectionStringData connection)
        {
            _dataAccess = dataAccess;
            _connection = connection;
        }

        public Task<int> CreateEmployee(Employee employee)
        {
            return _dataAccess.SaveData(
                "dbo.spEmployees_Insert",
                new
                {
                    employee.UserId,
                    employee.Email,
                    employee.IsActive
                },
                _connection.SqlConnectionName
            );
        }

        public async Task<Employee?> GetEmployeeById(int id)
        {
            var result = await _dataAccess.LoadData<Employee, dynamic>(
                "dbo.spEmployees_GetById",
                new { Id = id },
                _connection.SqlConnectionName
            );

            return result.FirstOrDefault();
        }

        public async Task<Employee?> GetEmployeeByUserId(string userId)
        {
            var result = await _dataAccess.LoadData<Employee, dynamic>(
                "dbo.spEmployees_GetByUserId",
                new { UserId = userId },
                _connection.SqlConnectionName
            );

            return result.FirstOrDefault();
        }

        public Task<List<Employee>> GetAllEmployees()
        {
            return _dataAccess.LoadData<Employee, dynamic>(
                "dbo.spEmployees_GetAll",
                new { },
                _connection.SqlConnectionName
            );
        }

        public Task<int> UpdateEmployee(Employee employee)
        {
            return _dataAccess.SaveData(
                "dbo.spEmployees_Update",
                new
                {
                    employee.Id,
                    employee.IsActive,
                    employee.UserId
                },
                _connection.SqlConnectionName
            );
        }

        public Task<int> DeleteEmployee(int id)
        {
            return _dataAccess.SaveData(
                "dbo.spEmployees_Delete",
                new { Id = id },
                _connection.SqlConnectionName
            );
        }
    }
}
    
