using DataLibrary.Models;

namespace UtilityManagerProjects.Models;

public sealed class DailyUsage
{
    public int MeterId { get; init; }
    public string MeterName { get; init; } = "";
    public MeterType MeterType { get; init; }
    public string DepartmentName { get; init; } = "";
    public string AreaName { get; init; } = "";
    public DateTime ReadingDate { get; init; }
    public decimal Usage { get; init; }
    public int ClosingReadingId { get; init; }
    public int OpeningReadingId { get; init; }
}

public static class IntervalConsumption
{
    // The counter at the end of the previous day is the baseline. Usage is
    // allocated to (previous capture date, current capture date], in calendar days.
    public static List<DailyUsage> Allocate(IEnumerable<MeterReading> readings, DateTime start, DateTime end)
    {
        var result = new List<DailyUsage>();
        foreach (var group in readings.Where(r => !r.IsDeleted).GroupBy(r => r.PhysicalMeterId ?? r.MeterId))
        {
            var captures = group.GroupBy(r => r.ReadingDate.Date)
                .Select(g => g.OrderByDescending(r => r.ReadingDate).ThenByDescending(r => r.Id).First())
                .OrderBy(r => r.ReadingDate).ToList();
            for (var i = 1; i < captures.Count; i++)
            {
                var previous = captures[i - 1]; var current = captures[i];
                var days = (current.ReadingDate.Date - previous.ReadingDate.Date).Days;
                if(current.Unit!=previous.Unit || !MeterUnits.Valid(current.Unit,current.MeterType))continue;
                var delta = (current.CurrentReading - previous.CurrentReading)*MeterUnits.Factor(current.Unit,current.MeterType);
                if (days <= 0 || delta < 0) continue; // A reset needs a new baseline, never negative usage.
                var first = previous.ReadingDate.Date.AddDays(1) > start.Date ? previous.ReadingDate.Date.AddDays(1) : start.Date;
                var last = current.ReadingDate.Date < end.Date ? current.ReadingDate.Date : end.Date;
                var average = delta / days;
                for (var offset = 0; offset <= (last - first).Days; offset++)
                {
                    var date = first.AddDays(offset);
                    result.Add(new DailyUsage { MeterId = current.MeterId, MeterName = current.MeterName,
                        MeterType = current.MeterType, DepartmentName = current.DepartmentName, AreaName = current.AreaName,
                        ReadingDate = date, ClosingReadingId = current.Id, OpeningReadingId = previous.Id,
                        Usage = date == current.ReadingDate.Date ? delta - average * (days - 1) : average });
                }
            }
        }
        return result;
    }
}

public sealed class ConsumptionScope
{
    public string Label { get; init; } = "";
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public HashSet<int> MeterIds { get; init; } = [];
    public List<string> Missing { get; init; } = [];
    public List<string> CoverageIssues { get; set; } = [];
    public List<DailyUsage> Allocated { get; init; } = [];
    public List<DateTime> Dates => Enumerable.Range(0, (End.Date - Start.Date).Days + 1).Select(i => Start.Date.AddDays(i)).ToList();
    public List<DailyUsage> CompleteRows
    {
        get
        {
            if (Missing.Count > 0 || MeterIds.Count == 0) return [];
            return Allocated.Where(r => MeterIds.Contains(r.MeterId)).GroupBy(r => r.ReadingDate.Date)
                .Where(g => g.Select(r => r.MeterId).Distinct().Count() == MeterIds.Count).SelectMany(g => g).ToList();
        }
    }
    public int CoveredDays => CompleteRows.Select(r => r.ReadingDate).Distinct().Count();
    public bool IsComplete => CoveredDays == Dates.Count;
    // Subtotal each counter first so fractional daily rates conserve its interval before combining meters.
    public decimal Total => CompleteRows.GroupBy(r=>r.MeterId).Sum(m=>m.Sum(r=>r.Usage));
    public decimal Average => CoveredDays == 0 ? 0 : Total / CoveredDays;
    public decimal CoveragePercent => Dates.Count == 0 ? 0 : 100m * CoveredDays / Dates.Count;
    public List<decimal?> Series()
    {
        var values = CompleteRows.GroupBy(r => r.ReadingDate).ToDictionary(g => g.Key, g => g.Sum(r => r.Usage));
        return Dates.Select(date => values.TryGetValue(date, out var value) ? (decimal?)value : null).ToList();
    }
}

public sealed class WaterSupplyRow
{
    public string Label { get; init; } = "";
    public string Role { get; init; } = "";
    public ConsumptionScope Flow { get; init; } = new();
}
public sealed class WaterAreaRow
{
    public string Area { get; init; } = "";
    public bool HasMunicipalSupply { get; init; }
    public bool HasBoreholeSupply { get; init; }
    public bool HasCombinedMeter { get; init; }
    public ConsumptionScope Combined { get; init; } = new();
    public ConsumptionScope Municipal { get; init; } = new();
    public ConsumptionScope Borehole { get; init; } = new();
}
public sealed class ConsumptionPeriod
{
    public ConsumptionScope MunicipalReceipt { get; init; } = new();
    public BoreholeFlowPair? BoreholePair { get; init; }
    public List<WaterSupplyRow> WaterSupplies { get; init; } = [];
    public List<WaterAreaRow> WaterAreas { get; init; } = [];
    public int CapturesInPeriod { get; init; }
    public int StartingOnlyMeterCount { get; init; }
    public List<DailyUsage> AllDaily { get; init; } = [];
    public ConsumptionScope Water { get; init; } = new();
    public ConsumptionScope Electricity { get; init; } = new();
    public List<DailyUsage> SummaryRows => Water.CompleteRows.Concat(Electricity.CompleteRows).ToList();
    public int CoveredDays
    {
        get
        {
            var scopes=new[]{Water,Electricity}.Where(s=>s.MeterIds.Count>0||s.Missing.Count>0).ToList();
            if(scopes.Count==0) return 0;
            var dates=scopes[0].CompleteRows.Select(r=>r.ReadingDate).ToHashSet();
            foreach(var scope in scopes.Skip(1)) dates.IntersectWith(scope.CompleteRows.Select(r=>r.ReadingDate));
            return dates.Count;
        }
    }
    public decimal CoveragePercent => Water.Dates.Count==0?0:100m*CoveredDays/Water.Dates.Count;
}
