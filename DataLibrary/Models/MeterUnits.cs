namespace DataLibrary.Models;

public static class MeterUnits
{
    public static string Default(MeterType type) => type == MeterType.Water ? "L" : "kWh";
    public static string Normalize(string? unit, MeterType type) => string.IsNullOrWhiteSpace(unit) ? Default(type) : unit.Trim().ToLowerInvariant() switch
    {
        "l" or "litre" or "litres" or "liter" or "liters" => "L",
        "m3" or "m³" => "m³", "kl" => "kL", "kwh" => "kWh", _ => unit.Trim()
    };
    public static bool Valid(string? unit, MeterType type) => type == MeterType.Water ? Normalize(unit,type) is "L" or "m³" or "kL" : Normalize(unit,type)=="kWh";
    public static decimal Factor(string? unit, MeterType type) => Valid(unit,type) ? Normalize(unit,type) is "m³" or "kL" ? 1000m : 1m : throw new ArgumentException("Select a volume unit for water (L, kL or m³). m² measures area, not water volume.");
    public static bool ValidCounter(decimal value) => value>=0 && value<=9999999999999.999999m && decimal.Round(value,6)==value;
}

public enum MeterFlowDirection { Unconfirmed=0, Inlet=1, Outlet=2 }
