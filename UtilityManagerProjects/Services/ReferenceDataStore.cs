using DataLibrary.Db;
namespace UtilityManagerProjects.Services;

public sealed class ReferenceDataStore(IDataAccess data, ConnectionStringData connection)
{
    public sealed class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? StationId { get; set; }
        public string StationName { get; set; } = "";
        public bool IsActive { get; set; }
    }
    public Task<List<Row>> Areas() => data.LoadData<Row, object>("dbo.spAdminAreas_All", new {}, connection.SqlConnectionName);
    public Task<List<Row>> Locations() => data.LoadData<Row, object>("dbo.spAdminLocations_All", new {}, connection.SqlConnectionName);
    public Task SaveArea(int? id,string name,int stationId,bool isActive) => data.SaveData("dbo.spAdminArea_Save",new {Id=id,AreaName=name.Trim(),StationId=stationId,IsActive=isActive},connection.SqlConnectionName);
    public Task SaveLocation(int? id,string name,bool isActive) => data.SaveData("dbo.spAdminLocation_Save",new {Id=id,StationName=name.Trim(),IsActive=isActive},connection.SqlConnectionName);
    public Task DeleteArea(int id) => data.SaveData("dbo.spAdminArea_Delete",new {Id=id},connection.SqlConnectionName);
    public Task DeleteLocation(int id) => data.SaveData("dbo.spAdminLocation_Delete",new {Id=id},connection.SqlConnectionName);
}
