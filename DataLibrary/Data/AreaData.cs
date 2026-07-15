using Dapper;
using DataLibrary.Db;
using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace DataLibrary.Data
{
    public class AreaData : IAreaData
    {
        private readonly IDataAccess dataAccess;
        private readonly ConnectionStringData connectionString;

        public AreaData(IDataAccess dataAccess, ConnectionStringData connectionString)
        {
            this.dataAccess = dataAccess;
            this.connectionString = connectionString;
        }

        public Task<List<Models.Area>> GetAreas()
        {
            return dataAccess.LoadData<Area, dynamic>("dbo.spArea_All",
                new { }, connectionString.SqlConnectionName);

        }

        public async Task<int> InsertAreas(Area area)
        {
            DynamicParameters p = new DynamicParameters();

            p.Add("AreaName", area.AreaName);
            p.Add("StationId",area.StationId);
            p.Add("DepartmentId", area.DepartmentId);
            p.Add("Id",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output);

            await dataAccess.SaveData("spArea_Insert",
               p,
                connectionString.SqlConnectionName);
            return p.Get<int>("Id");
        }
       public Task<int> DeleteArea(int areaId)
        {
            return dataAccess.SaveData("dbo.spArea_Delete",
                new { Id = areaId },
                connectionString.SqlConnectionName);
              
        }

        public Task<int> UpdateArea(int areaId , string areaName, string discription, int stationId, int depId,int updatedBy)
        {
            return dataAccess.SaveData("dbo.spArea_Update",
                new
                {
                    Id = areaId,
                    AreaName =areaName,
                    AreadDiscription = discription,
                    StationId = stationId,
                    DepartmentId = depId,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = updatedBy // You can replace this with actual user info if available
                },
                connectionString.SqlConnectionName);
        }
        
       
    }
}
