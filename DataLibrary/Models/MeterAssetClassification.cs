using System.ComponentModel.DataAnnotations;
namespace DataLibrary.Models;
public enum AssetKind { Meter = 1, Tank = 2 }
public enum WaterNetworkRole
{
    Source = 1, Transfer = 2, Distribution = 3,
    [Display(Name = "End use")] EndUse = 4, Storage = 5
}
public static class MeterAssetRules
{
    public static string? Validate(AssetKind kind, WaterNetworkRole role, MeterType type, decimal? capacity)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(role)) return "Select a valid asset kind and network role.";
        if (kind == AssetKind.Tank && type != MeterType.Water) return "Tanks must use the Water utility type.";
        if (kind == AssetKind.Tank && role is not (WaterNetworkRole.Source or WaterNetworkRole.Storage or WaterNetworkRole.Distribution)) return "A tank must have a Source, Storage or Distribution role.";
        if (kind == AssetKind.Meter && role == WaterNetworkRole.Storage) return "Use Tank for storage; a flow meter measures Source, Transfer, Distribution or End use.";
        if (capacity.HasValue && (capacity <= 0 || capacity > 999999999999.99m)) return "Tank capacity must be greater than zero and within the supported range.";
        if (kind == AssetKind.Meter && capacity.HasValue) return "Capacity applies only to tanks.";
        return null;
    }
}
