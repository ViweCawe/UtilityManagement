using System.Text.Json;

namespace UtilityManagerProjects.Models;

/// <summary>Physical routes from the Figma map, independent of consumption readings.</summary>
public sealed class WaterNetwork
{
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
        network.Validate();
        return network;
    }

    public void Validate()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id) || !ids.Add(node.Id) ||
                string.IsNullOrWhiteSpace(node.Label) || string.IsNullOrWhiteSpace(node.FigmaNodeId) ||
                node.Station is not ("Lower" or "Top") ||
                node.Kind is not ("source" or "storage" or "distribution" or "usage" or "transfer" or "unresolved") ||
                !double.IsFinite(node.X) || !double.IsFinite(node.Y) || node.X < 0 || node.Y < 0)
                throw new InvalidDataException($"Invalid water network node: {node.Id}.");
        }

        var pairs = new HashSet<(string, string)>();
        foreach (var edge in Connections)
        {
            if (!ids.Contains(edge.From) || !ids.Contains(edge.To) || edge.From == edge.To ||
                !pairs.Add((edge.From, edge.To)) || string.IsNullOrWhiteSpace(edge.FigmaConnectorId) ||
                edge.Kind is not ("supply" or "transfer" or "rain" or "backup") ||
                (edge.Kind == "backup" && string.IsNullOrWhiteSpace(edge.Condition)))
                throw new InvalidDataException($"Invalid water connection: {edge.From} -> {edge.To}.");
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
    }
}

public sealed class WaterNetworkNode
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Station { get; init; } = "";
    public string Kind { get; init; } = "";
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
    public string FigmaConnectorId { get; init; } = "";
}
