using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DataLibrary.Db
{
    public class SqlDb : IDataAccess
    {
        private readonly IConfiguration config;
        // C#
        public SqlDb(IConfiguration config)
        {
            this.config = config;
        }
        public async Task<List<T>> LoadData<T, U>(string storeProcedure, U parameters, string connectionStringName)
        {
            string connectionString = config.GetConnectionString(connectionStringName);

            using (IDbConnection connection = new SqlConnection(connectionString))
            {
                var rows = await connection.QueryAsync<T>(storeProcedure, parameters,
                                                          commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
        }
        public async Task<int> SaveData<T>(string storedProcdure, T parameters, string connectionStringName)
        {
            string connectionString = config.GetConnectionString(connectionStringName);
            using (IDbConnection connection = new SqlConnection(connectionString))
            {

                return await connection.ExecuteAsync(storedProcdure, parameters,
                                                     commandType: CommandType.StoredProcedure);
            }
        }
    }
}