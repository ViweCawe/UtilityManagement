using System.Text.Json;

namespace UtilityManagerProjects.Models;

/// <summary>Editable physical supply routes, independent of consumption readings.</summary>
public sealed class WaterNetwork
{
    public int CircuitDefinitionsVersion { get; set; }
    public string SourceUrl { get; init; } = "";
    public string ReviewedOn { get; init; } = "";
    public List<WaterNetworkNode> Nodes { get; init; } = [];
    public List<WaterNetworkConnection> Connections { get; init; } = [];
    public List<string> OpenQuestions { get; init; } = [];

    public static WaterNetwork Load(Stream stream)
    {
        var network = JsonSerializer.Deserialize<WaterNetwork>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Water network is empty.");
        if(network.CircuitDefinitionsVersion<1) {
            foreach(var node in network.Nodes??[]) if(node is not null) node.UsageArea ??= WaterAccounting.Area(node);
            for(var i=0;i<(network.Connections?.Count??0);i++) {
                var edge=network.Connections![i];if(edge is null) continue;
                if(edge.From=="main-tank" && edge.To=="staff-toilets-lower")
                    network.Connections[i]=new(){From=edge.From,To=edge.To,Kind="supply",Purpose="Basins; backup flushing when borehole runs out",Condition="Basins normally use municipal water. Municipal flushing only when borehole water runs out.",SourceFilter="municipal",FigmaConnectorId=edge.FigmaConnectorId};
                else if(edge.From=="borehole-out" && edge.To is "workshop" or "staff-toilets-lower") {edge.Purpose="Toilet flushing";edge.SourceFilter="borehole";}
                else if(edge.From=="main-tank" && edge.To=="workshop") {edge.Purpose="Basins; backup flushing when borehole runs out";edge.SourceFilter="municipal";}
                else if(edge.From=="workshop" && edge.To is "workshop-basins" or "workshop-fountains" or "old-cable-car") {edge.SourceFilter="municipal";edge.Purpose="Clean-water circuit";}
            }
            network.CircuitDefinitionsVersion=1;
        }
        network.Validate();
        return network;
    }

    public void Validate()
    {
        if (Nodes is null || Connections is null || Nodes.Count is < 1 or > 500 || Connections.Count > 1500)
            throw new InvalidDataException("Use between 1 and 500 points and at most 1500 connections.");
        var meterIds = new HashSet<int>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in Nodes)
        {
            if (node is null || string.IsNullOrWhiteSpace(node.Id) || !ids.Add(node.Id) ||
                string.IsNullOrWhiteSpace(node.Label) || node.Label.Length > 100 ||
                !System.Text.RegularExpressions.Regex.IsMatch(node.Id, "^[a-z0-9-]{1,80}$") ||
                (node.Note?.Length ?? 0) > 1000 || (node.UsageArea?.Length ?? 0)>100 ||
                node.ReportingRole is not (null or "" or "main" or "borehole-inlet" or "borehole-outlet" or "hydrant") ||
                node.SupplyOrigin is not (null or "" or "municipal" or "borehole" or "rain" or "other") ||
                (node.MeterId.HasValue && (!node.CanLinkMeter || node.MeterId <= 0 || !meterIds.Add(node.MeterId.Value))) ||
                node.Station is not ("Lower" or "Top") ||
                node.Kind is not ("source" or "storage" or "distribution" or "usage" or "transfer" or "unresolved") ||
                !double.IsFinite(node.X) || !double.IsFinite(node.Y) || node.X < 0 || node.Y < 0 || node.X > 1200 || node.Y > 100000)
                throw new InvalidDataException($"Invalid water network node: {node?.Id}.");
        }

        var pairs = new HashSet<(string, string)>();
        foreach (var edge in Connections)
        {
            if (edge is null || !ids.Contains(edge.From) || !ids.Contains(edge.To) || edge.From == edge.To ||
                !pairs.Add((edge.From, edge.To)) || (edge.Condition?.Length ?? 0) > 200 || (edge.Purpose?.Length ?? 0)>120 || edge.SourceFilter is not (null or "" or "municipal" or "borehole" or "rain" or "other") ||
                edge.Kind is not ("supply" or "transfer" or "rain" or "backup") ||
                (edge.Kind == "backup" && string.IsNullOrWhiteSpace(edge.Condition)))
                throw new InvalidDataException($"Invalid water connection: {edge?.From} -> {edge?.To}.");
        }

        foreach (var node in Nodes.Where(n=>n.IsExternalSupply==true))
            if(!node.CanLinkMeter || node.Kind is not ("source" or "storage") || Connections.Any(e=>e.To==node.Id && e.Kind!="backup"))
                throw new InvalidDataException("An external supply must be a metered Source or Storage point with no normal upstream connection.");
        var reportingRoles=new HashSet<string>();
        foreach(var node in Nodes) {
            var role=WaterAccounting.Role(node);
            if(role.Length>0 && (!node.CanLinkMeter || !reportingRoles.Add(role)))
                throw new InvalidDataException("Each reporting boundary needs one distinct metered point: Main Meter, Borehole In, Borehole Out or Fire Hydrant.");
            if(role is "main" or "hydrant" or "borehole-inlet")
                if(Connections.Any(e=>e.To==node.Id&&e.Kind!="backup")) throw new InvalidDataException("An inlet reporting meter must not have a normal upstream connection.");
        }
        // A cycle would make this documented source-to-consumer network misleading.
        var visiting = new HashSet<string>();
        var visited = new HashSet<string>();
        void Visit(string id)
        {
            if (visited.Contains(id)) return;
            if (!visiting.Add(id)) throw new InvalidDataException("Water network contains a cycle.");
            foreach (var edge in Connections.Where(e => e.From == id)) Visit(edge.To);
            visiting.Remove(id);
            visited.Add(id);
        }
        foreach (var id in ids) Visit(id);
        foreach(var edge in Connections.Where(e=>!string.IsNullOrEmpty(e.SourceFilter)))
            if(!WaterAccounting.Origins(this,edge.From).Contains(edge.SourceFilter!)) throw new InvalidDataException("A route cannot carry a source its upstream point does not supply. Check the source type and circuit restriction.");
        foreach(var node in Nodes.Where(n=>WaterAccounting.Role(n).Length>0)) {
            var required=WaterAccounting.Role(node) is "main" or "hydrant"?"municipal":"borehole";
            var origins=WaterAccounting.Origins(this,node.Id);
            if(origins.Count!=1||!origins.Contains(required)) throw new InvalidDataException("Reporting meters must measure their declared source only. A combined municipal/borehole meter cannot represent either source total.");
        }
    }
}

public sealed class WaterNetworkNode
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Station { get; init; } = "";
    public string Kind { get; init; } = "";
    public bool CanLinkMeter { get; init; } = true;
    public int? MeterId { get; set; }
    public bool? IsExternalSupply { get; set; }
    public string? ReportingRole { get; set; }
    public string? SupplyOrigin { get; set; }
    public string? UsageArea { get; set; }
    public string Note { get; init; } = "";
    public string FigmaNodeId { get; init; } = "";
    public double X { get; init; }
    public double Y { get; init; }
}

public sealed class WaterNetworkConnection
{
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public string Kind { get; init; } = "supply";
    public string Condition { get; init; } = "";
    public string Purpose { get; set; } = "";
    public string? SourceFilter { get; set; }
    public string FigmaConnectorId { get; init; } = "";
}

public static class WaterAccounting
{
    public static string Role(WaterNetworkNode n) => n.ReportingRole ?? (n.Id switch {"main-tank"=>"main","borehole-in"=>"borehole-inlet","borehole-out"=>"borehole-outlet","fire-hydrant"=>"hydrant",_=>""});
    public static string Area(WaterNetworkNode n) => n.UsageArea is null?n.Id switch {"workshop"=>"Workshop","staff-toilets-lower"=>"Level 1 Visitor Toilets",_=>""}:n.UsageArea.Trim();
    public static string Origin(WaterNetworkNode n) => !string.IsNullOrWhiteSpace(n.SupplyOrigin)?n.SupplyOrigin:Role(n) switch {"main" or "hydrant"=>"municipal","borehole-inlet" or "borehole-outlet"=>"borehole",_=>"other"};
    public static HashSet<string> Origins(WaterNetwork map,string id)
    {
        var cache=new Dictionary<string,HashSet<string>>();var visiting=new HashSet<string>();
        HashSet<string> Visit(string current) {
            if(cache.TryGetValue(current,out var cached)) return new(cached);
            if(!visiting.Add(current)) return new(){"other"};
            var result=new HashSet<string>();var node=map.Nodes.Single(n=>n.Id==current);
            var parents=map.Connections.Where(e=>e.To==current).ToList();
            if(parents.Count==0) result.Add(Origin(node));
            foreach(var edge in parents) {
                var sources=edge.Kind=="rain"?new HashSet<string>{"rain"}:Visit(edge.From);
                if(!string.IsNullOrEmpty(edge.SourceFilter)) sources.IntersectWith(new[]{edge.SourceFilter});
                result.UnionWith(sources);
            }
            visiting.Remove(current);cache[current]=new(result);return result;
        }
        return Visit(id);
    }
}
