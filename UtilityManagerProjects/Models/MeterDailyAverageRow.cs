using DataLibrary.Models;
namespace UtilityManagerProjects.Models;
public sealed class MeterDailyAverageRow
{
 public int MeterId { get; set; }
 public string MeterName { get; set; } = "";
 public MeterType MeterType { get; set; }
 public string DepartmentName { get; set; } = "";
 public int? LatestCounter { get; set; }
 public DateTime? LatestCounterDate { get; set; }
 public string CalculationStatus { get; set; } = "";
 public int ReadingCount { get; set; }
 public decimal TotalUsage { get; set; }
 public decimal DailyAverage { get; set; }
 public int CoveredDays { get; set; }
 public string Unit => MeterType==MeterType.Water?"L":"kWh";
}
