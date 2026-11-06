using DataLibrary.Models;
namespace UtilityManagerProjects.Models;
public sealed class MeterDailyAverageRow
{
 public int MeterId { get; set; }
 public string MeterName { get; set; } = "";
 public MeterType MeterType { get; set; }
 public string DepartmentName { get; set; } = "";
 public decimal? LatestCounter { get; set; }
 public string CounterUnit { get; set; } = "L";
 public decimal? DisplayLatestCounter => LatestCounter*(MeterUnits.Valid(CounterUnit,MeterType)?MeterUnits.Factor(CounterUnit,MeterType):1m);
 public string CounterDisplayUnit => MeterUnits.Valid(CounterUnit,MeterType)?Unit:CounterUnit;
 public bool HasConvertedCounter => MeterUnits.Valid(CounterUnit,MeterType)&&MeterUnits.Factor(CounterUnit,MeterType)!=1m;
 public DateTime? LatestCounterDate { get; set; }
 public string CalculationStatus { get; set; } = "";
 public string ReadingPurpose { get; set; } = "";
 public bool IsConfirmedConsumption { get; set; }
 public string SupplyDescription { get; set; } = "";
 public int ReadingCount { get; set; }
 public decimal TotalUsage { get; set; }
 public decimal DailyAverage { get; set; }
 public int CoveredDays { get; set; }
 public string Unit => MeterType==MeterType.Water?"L":"kWh";
}
