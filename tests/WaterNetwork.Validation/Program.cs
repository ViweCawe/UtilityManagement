using System.Text;
using System.Text.Json.Nodes;
using UtilityManagerProjects.Models;

var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "water-network.json"));
using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
{
    var map = WaterNetwork.Load(stream);
    if (map.Nodes.Count != 33 || map.Connections.Count != 32)
        throw new Exception("Unexpected source map inventory.");
}

void Reject(string name, Action<JsonObject> corrupt)
{
    var document = JsonNode.Parse(json)!.AsObject();
    corrupt(document);
    try
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToJsonString()));
        WaterNetwork.Load(stream);
    }
    catch (InvalidDataException)
    {
        Console.WriteLine($"PASS: {name}");
        return;
    }
    throw new Exception($"Invalid data was accepted: {name}.");
}

Reject("duplicate node", d => d["nodes"]!.AsArray().Add(d["nodes"]![0]!.DeepClone()));
Reject("dangling connection", d => d["connections"]![0]!["to"] = "missing-point");
Reject("duplicate connection", d => d["connections"]!.AsArray().Add(d["connections"]![0]!.DeepClone()));
Reject("cycle", d => d["connections"]!.AsArray().Add(new JsonObject
{
    ["from"] = "header", ["to"] = "main-tank", ["kind"] = "supply", ["figmaConnectorId"] = "test"
}));
Reject("backup without condition", d =>
{
    var backup = d["connections"]!.AsArray()[0]!;
    backup["kind"] = "backup";
    backup["condition"] = "";
});
Reject("invalid station", d => d["nodes"]![0]!["station"] = "Unknown");
Console.WriteLine("PASS: source map loads and all six invalid-data guards reject bad input.");

if (DataLibrary.Models.MeterAssetRules.Validate(DataLibrary.Models.AssetKind.Tank, DataLibrary.Models.WaterNetworkRole.Storage, DataLibrary.Models.MeterType.Water, 1000m) is not null) throw new Exception("Valid storage tank rejected");
if (DataLibrary.Models.MeterAssetRules.Validate(DataLibrary.Models.AssetKind.Tank, DataLibrary.Models.WaterNetworkRole.EndUse, DataLibrary.Models.MeterType.Water, null) is null) throw new Exception("Tank end-use accepted");
if (DataLibrary.Models.MeterAssetRules.Validate(DataLibrary.Models.AssetKind.Tank, DataLibrary.Models.WaterNetworkRole.Storage, DataLibrary.Models.MeterType.Electricity, null) is null) throw new Exception("Electricity tank accepted");
if (DataLibrary.Models.MeterAssetRules.Validate(DataLibrary.Models.AssetKind.Meter, DataLibrary.Models.WaterNetworkRole.Storage, DataLibrary.Models.MeterType.Water, null) is null) throw new Exception("Storage meter accepted");
if (DataLibrary.Models.MeterAssetRules.Validate(DataLibrary.Models.AssetKind.Meter, DataLibrary.Models.WaterNetworkRole.Source, DataLibrary.Models.MeterType.Water, 5m) is null) throw new Exception("Meter capacity accepted");
Console.WriteLine("PASS: asset classification guards");

Reject("null point", d => d["nodes"]!.AsArray().Add((JsonNode?)null));
Reject("duplicate meter assignment", d => { d["nodes"]![0]!["meterId"] = 7; d["nodes"]![1]!["meterId"] = 7; });
Reject("unmetered point with assignment", d => { d["nodes"]![0]!["canLinkMeter"] = false; d["nodes"]![0]!["meterId"] = 7; });
var editable = new WaterNetwork { Nodes = [ new WaterNetworkNode { Id="new-source", Label="New source", Station="Lower", Kind="source", MeterId=4 } ] };
editable.Validate();
Console.WriteLine("PASS: editable points need no Figma ID; invalid assignments and null points rejected.");

var legacy=JsonNode.Parse(json)!.AsObject();legacy["circuitDefinitionsVersion"]=0;
var level1=legacy["connections"]!.AsArray().First(e=>e!["from"]!.GetValue<string>()=="main-tank"&&e["to"]!.GetValue<string>()=="staff-toilets-lower")!;
level1["kind"]="backup";level1.AsObject().Remove("purpose");level1.AsObject().Remove("sourceFilter");
using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(legacy.ToJsonString()))) {
    var migrated=WaterNetwork.Load(stream);
    var route=migrated.Connections.Single(e=>e.From=="main-tank"&&e.To=="staff-toilets-lower");
    if(route.Kind!="supply"||!route.Purpose.Contains("Basins")||migrated.CircuitDefinitionsVersion!=1)throw new Exception("Legacy circuit clarification failed");
}
Console.WriteLine("PASS: legacy maps acquire normal municipal basins and borehole flushing circuit definitions.");
