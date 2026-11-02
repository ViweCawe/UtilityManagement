using System.Text.Json;
using DataLibrary.Db;
using UtilityManagerProjects.Models;

namespace UtilityManagerProjects.Services;

public sealed class WaterNetworkStore(IDataAccess data, ConnectionStringData connection, IWebHostEnvironment environment)
{
    public sealed class Document
    {
        public string NetworkJson { get; set; } = "";
        public int Revision { get; set; }
    }

    public async Task<(WaterNetwork Network, int Revision)> LoadAsync()
    {
        var records = await data.LoadData<Document, object>("dbo.spWaterNetworkDocument_Get", new { }, connection.SqlConnectionName);
        var document = records.SingleOrDefault();
        if (document is not null && !string.IsNullOrEmpty(document.NetworkJson))
        {
            using var saved = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(document.NetworkJson));
            return (WaterNetwork.Load(saved), document.Revision);
        }
        using var seed = environment.ContentRootFileProvider.GetFileInfo("App_Data/water-network.json").CreateReadStream();
        return (WaterNetwork.Load(seed), document?.Revision ?? 0);
    }

    public Task<int> SaveAsync(WaterNetwork network, int revision, string userId)
    {
        network.Validate();
        return data.SaveData("dbo.spWaterNetworkDocument_Save", new
        {
            NetworkJson = JsonSerializer.Serialize(network, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            ExpectedRevision = revision,
            UpdatedBy = userId
        }, connection.SqlConnectionName);
    }
}
