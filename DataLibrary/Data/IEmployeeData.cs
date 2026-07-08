using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Data
{
    public interface IEmployeeData
    {
        Task<int> CreateEmployee(Employee employee);

        Task<Employee?> GetEmployeeById(int id);

        Task<Employee?> GetEmployeeByUserId(string userId);

        Task<List<Employee>> GetAllEmployees();

        Task<int> UpdateEmployee(Employee employee);

        Task<int> DeleteEmployee(int id);
    }
}
