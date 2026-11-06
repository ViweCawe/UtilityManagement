using DataLibrary.Models;
namespace UtilityManagerProjects.Models;

// A replacement is the same measurement boundary with a new physical counter.
public sealed class MeterLineage
{
    readonly Dictionary<int,Meter> meters;
    readonly Dictionary<int,int> successor;
    public MeterLineage(IEnumerable<Meter> source)
    {
        meters=source.ToDictionary(m=>m.Id);successor=[];
        foreach(var meter in meters.Values.Where(m=>m.ReplacesMeterId.HasValue))
        {
            if(!meters.TryGetValue(meter.ReplacesMeterId!.Value,out var old)||!successor.TryAdd(old.Id,meter.Id)||old.IsActive||old.MeterType!=meter.MeterType||old.FlowDirection!=meter.FlowDirection||old.DepartmentId!=meter.DepartmentId||old.AreaId!=meter.AreaId||old.StationId!=meter.StationId||old.NetworkRole!=meter.NetworkRole||old.AssetKind!=meter.AssetKind||!meter.InstalledOn.HasValue||old.RetiredOn!=meter.InstalledOn)
                throw new InvalidDataException("Replacement chain needs one successor, matching measurement boundary and a recorded cutover; the previous meter must be inactive.");
        }
        foreach(var id in meters.Keys)Resolve(id);
    }
    public int Resolve(int id)
    {
        var seen=new HashSet<int>();
        while(successor.TryGetValue(id,out var next)){if(!seen.Add(id))throw new InvalidDataException("Replacement chain contains a cycle.");id=next;}
        return id;
    }
    public List<Meter> Family(int id)=>meters.Values.Where(m=>Resolve(m.Id)==Resolve(id)).OrderBy(m=>m.InstalledOn??DateTime.MinValue).ThenBy(m=>m.Id).ToList();
    public bool InService(MeterReading r)=>meters.TryGetValue(r.MeterId,out var meter)&&(!meter.InstalledOn.HasValue||r.ReadingDate>=meter.InstalledOn)&&(!meter.RetiredOn.HasValue||r.ReadingDate<=meter.RetiredOn);
}
