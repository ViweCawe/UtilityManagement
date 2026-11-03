global using Microsoft.AspNetCore.Hosting;
global using Microsoft.Extensions.Configuration;
using System.Collections;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using DataLibrary.Data;
using DataLibrary.Db;
using DataLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using UtilityManagerProjects.Models;
using UtilityManagerProjects.Services;

static void Equal<T>(T expected,T actual,string description) { if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"{description}: expected {expected}, got {actual}"); }
static MeterReading Capture(int meter,DateTime date,int counter,int id=1) => new(){Id=id,MeterId=meter,ReadingDate=date,CurrentReading=counter,MeterType=MeterType.Water};
static ConsumptionScope Scope(List<DailyUsage> rows,DateTime start,DateTime end,params int[] meters)=>new(){Start=start,End=end,MeterIds=meters.ToHashSet(),Allocated=rows};
var day=new DateTime(2025,1,1);
var weekly=new[]{Capture(1,day,1000),Capture(1,day.AddDays(7),1700,2),Capture(1,day.AddDays(14),3100,3),Capture(1,day.AddDays(21),3450,4)};
var allocated=IntervalConsumption.Allocate(weekly,day.AddDays(1),day.AddDays(21));
Equal(21,allocated.Count,"one value for each day between captures");
Equal(100m,allocated[0].Usage,"first weekly average");Equal(200m,allocated[7].Usage,"next weekly average rises");Equal(50m,allocated[14].Usage,"next weekly average falls");
Equal(2450m,allocated.Sum(d=>d.Usage),"allocated intervals conserve counter changes");
Equal(300m,IntervalConsumption.Allocate(weekly,day.AddDays(2),day.AddDays(4)).Sum(d=>d.Usage),"clipping uses closing readings outside the selected dates");
Equal(0,IntervalConsumption.Allocate(weekly.Take(1),day,day.AddDays(10)).Count,"first capture is only a baseline");
var tail=Scope(IntervalConsumption.Allocate(weekly.Take(2),day.AddDays(1),day.AddDays(9)),day.AddDays(1),day.AddDays(9),1);
Equal(7,tail.CoveredDays,"no extrapolation past latest capture");Equal(100m,tail.Average,"daily average excludes unknown tail");Equal(null,tail.Series()[8],"unknown day is null, never zero");
var zero=IntervalConsumption.Allocate(new[]{Capture(1,day,100),Capture(1,day.AddDays(7),100,2)},day.AddDays(1),day.AddDays(7));
Equal(7,zero.Count,"zero measured flow has coverage");Equal(0m,Scope(zero,day.AddDays(1),day.AddDays(7),1).Series()[0]!.Value,"zero does not become unknown");
var fractional=IntervalConsumption.Allocate(new[]{Capture(1,day,0),Capture(1,day.AddDays(3),1,2)},day,day.AddDays(3));
Equal(1m,fractional.Sum(d=>d.Usage),"fractional daily rates conserve exactly");
var sameDay=IntervalConsumption.Allocate(new[]{Capture(1,day.AddHours(8),1000),Capture(1,day.AddHours(16),1010,2),Capture(1,day.AddDays(7),1710,3)},day,day.AddDays(7));
Equal(700m,sameDay.Sum(d=>d.Usage),"last capture per calendar day is the daily baseline");
var reset=IntervalConsumption.Allocate(new[]{Capture(1,day,100),Capture(1,day.AddDays(7),50,2),Capture(1,day.AddDays(14),120,3)},day,day.AddDays(14));
Equal(7,reset.Count,"negative interval is not converted to consumption");Equal(70m,reset.Sum(d=>d.Usage),"later reset baseline supports subsequent valid intervals");
var deleted=Capture(1,day.AddDays(3),90000,2);deleted.IsDeleted=true;
Equal(700m,IntervalConsumption.Allocate(new[]{weekly[0],deleted,weekly[1]},day,day.AddDays(7)).Sum(d=>d.Usage),"deleted capture is excluded before differencing");
var missing=Scope(allocated,day.AddDays(1),day.AddDays(7),1,99);
Equal(0,missing.CoveredDays,"a missing boundary meter prevents partial site totals");
Console.WriteLine("PASS: interval allocation, clipping, baseline, decimal precision, same-day captures, zero, reset, deletion, coverage and unknown-day cases.");

var fixture=new Fixture(day);
var service=fixture.Service();await service.LoadAsync();
var period=service.ForPeriod(day.AddDays(1),day.AddDays(7));
Equal(910m,period.Water.Total,"site supply is Main + Hydrant + Borehole incoming, never downstream flow");
Equal(130m,period.Water.Average,"site daily source flow");Equal(7,period.CoveredDays,"coverage does not count capture dates");
Equal(350m,service.ForPeriod(day.AddDays(1),day.AddDays(7),2).Water.Total,"specific meter drill-down");
Equal(406m,service.ForPeriod(day.AddDays(1),day.AddDays(7),department:"Ops").Water.Total,"department frontier excludes its selected descendants");
Equal(390m,service.ForPeriod(day.AddDays(2),day.AddDays(4)).Water.Total,"window wholly between captures retains source allocation");
fixture.Links.Remove("hydrant");var incomplete=fixture.Service();await incomplete.LoadAsync();
Equal(0,incomplete.ForPeriod(day.AddDays(1),day.AddDays(7)).Water.CoveredDays,"unmapped source cannot silently fall back to summing all meters");
fixture.Links["hydrant"]=3;
var report=new UtilityManagerProjects.Pages.Reports.IndexModel(fixture.Readings,fixture.Waste,fixture.Service()) { DateFilter="Custom",StartDate=day.AddDays(1),EndDate=day.AddDays(7) };
Attach(report);await report.OnGet();Equal(910m,report.WaterTotal,"Reports uses the shared site boundary");Equal(50m,report.MeterDailyAverages.Single(r=>r.MeterId==2).DailyAverage,"Reports retains downstream per-meter daily averages");
var env=new UtilityManagerProjects.Pages.Environmental.IndexModel(fixture.Readings,fixture.Waste,fixture.People,fixture.Service(),fixture.Config) { DateFilter="Custom",StartDate=day.AddDays(1),EndDate=day.AddDays(7) };
Attach(env);await env.OnGet();Equal(910m,env.WaterUsage,"Environmental matches Reports");Equal(13m,env.WaterPerPerson,"per-person denominator matches complete reporting interval");Equal(false,env.CarbonEstimateAvailable,"missing emissions factor does not invent a value");
var dashboard=new UtilityManagerProjects.Pages.Dashboard.DashboardModel(fixture.Readings,fixture.Service()) { DateFilter="Custom",StartDate=day.AddDays(1),EndDate=day.AddDays(7) };
Attach(dashboard);await dashboard.OnGet();Equal(910m,dashboard.WaterConsumption,"Dashboard matches Reports");
var list=new UtilityManagerProjects.Pages.MeterReadings.ListModel(fixture.Readings,fixture.Service()) { DateFilter="Custom",StartDate=day.AddDays(1),EndDate=day.AddDays(7) };
Attach(list);await list.OnGet();Equal(910m,list.WaterUsage,"Reading list summary matches site boundary");Equal(6,list.TotalReadings,"capture counts stay raw, not synthetic daily counts");
var file=(FileContentResult)await report.OnGetExportExcel();
using var archive=new ZipArchive(new MemoryStream(file.FileContents));
XDocument Xml(string path){using var stream=archive.GetEntry(path)!.Open();return XDocument.Load(stream);}
XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
var workbook=Xml("xl/workbook.xml");Equal(5,workbook.Descendants(ns+"sheet").Count(),"Excel includes raw captures and separate daily estimates");
var daily=Xml("xl/worksheets/sheet4.xml");
var included=daily.Descendants(ns+"row").Skip(1).Where(r=>r.Elements(ns+"c").Last().Value=="Yes").ToList();
var exportTotal=included.Sum(r=>decimal.Parse(r.Elements(ns+"c").ElementAt(4).Element(ns+"v")!.Value,System.Globalization.CultureInfo.InvariantCulture));
Equal(report.WaterTotal,exportTotal,"Excel daily boundary rows reconcile to on-screen total");
Console.WriteLine("PASS: boundary selection, department/meter filters, incomplete mappings, page agreement, raw capture counts and Excel reconciliation.");

var electricFixture=new Fixture(day);
electricFixture.Meters.Add(new Meter{Id=7,MeterName="Power",MeterType=MeterType.Electricity,IsActive=true});
electricFixture.History.Add(new MeterReading{Id=20,MeterId=7,MeterType=MeterType.Electricity,ReadingDate=day,CurrentReading=500});
electricFixture.History.Add(new MeterReading{Id=21,MeterId=7,MeterType=MeterType.Electricity,ReadingDate=day.AddDays(7),CurrentReading=1200});
electricFixture.Config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Environmental:GridEmissionFactorKgCo2ePerKwh","0.5"}}).Build();
var fullElectric=new UtilityManagerProjects.Pages.Environmental.IndexModel(electricFixture.Readings,electricFixture.Waste,electricFixture.People,electricFixture.Service(),electricFixture.Config){DateFilter="Custom",StartDate=day.AddDays(1),EndDate=day.AddDays(7)};
Attach(fullElectric);await fullElectric.OnGet();Equal(700m,fullElectric.ElectricityUsage,"electricity uses the interval allocator");Equal(true,fullElectric.ElectricityPerPersonAvailable,"complete electricity supports per-person reporting");Equal(true,fullElectric.CarbonEstimateAvailable,"configured carbon factor with complete coverage is usable");
var partialElectric=new UtilityManagerProjects.Pages.Environmental.IndexModel(electricFixture.Readings,electricFixture.Waste,electricFixture.People,electricFixture.Service(),electricFixture.Config){DateFilter="Custom",StartDate=day.AddDays(1),EndDate=day.AddDays(9)};
Attach(partialElectric);await partialElectric.OnGet();Equal(false,partialElectric.ElectricityPerPersonAvailable,"unknown tail suppresses misleading per-person calculation");Equal(false,partialElectric.CarbonEstimateAvailable,"unknown tail suppresses carbon estimate");
Console.WriteLine("PASS: electricity allocation, configured carbon factor and incomplete-coverage environmental guards.");

static void Attach(PageModel page) => page.PageContext=new PageContext { HttpContext=new DefaultHttpContext(),ViewData=new ViewDataDictionary(new EmptyModelMetadataProvider(),new ModelStateDictionary()) };

public class Fixture
{
    public Dictionary<string,int?> Links=new(){["main"]=1,["garage"]=2,["hydrant"]=3,["incoming"]=4,["out"]=5,["toilet"]=6};
    public IConfiguration Config=new ConfigurationBuilder().Build();
    public List<Meter> Meters=[];public List<MeterReading> History=[];
    public IMeterData MeterData; public IMeterReadingData Readings;public IWasteReadingData Waste;public IDailyPeopleCountData People;public IDataAccess Db;
    private WaterNetwork graph=new();
    public Fixture(DateTime day)
    {
        var ids=new[]{"main","garage","hydrant","incoming","out","toilet"};
        var kinds=new[]{"source","distribution","source","storage","distribution","usage"};
        var deltas=new[]{700,350,70,140,56,14};
        for(var i=0;i<6;i++)
        {
            var m=new Meter {Id=i+1,MeterName=ids[i],MeterType=MeterType.Water,DepartmentName=i is 1 or 4 or 5?"Ops":"Supply",AreaName="Lower",IsActive=true};Meters.Add(m);
            History.Add(new MeterReading{Id=i*2+1,MeterId=m.Id,MeterName=m.MeterName,MeterType=m.MeterType,DepartmentName=m.DepartmentName,AreaName=m.AreaName,ReadingDate=day,CurrentReading=1000,PreviousReading=1000,Usage=0,IsBaseline=true});
            History.Add(new MeterReading{Id=i*2+2,MeterId=m.Id,MeterName=m.MeterName,MeterType=m.MeterType,DepartmentName=m.DepartmentName,AreaName=m.AreaName,ReadingDate=day.AddDays(7),CurrentReading=1000+deltas[i],PreviousReading=1000,PreviousReadingDate=day,IntervalDays=7,Usage=deltas[i]});
        }
        graph=new WaterNetwork {Nodes=ids.Select((id,i)=>new WaterNetworkNode {Id=id,Label=id,Kind=kinds[i],Station="Lower",IsExternalSupply=i is 0 or 2 or 3}).ToList(),Connections=[new(){From="main",To="garage"},new(){From="incoming",To="out"},new(){From="out",To="toilet"},new(){From="main",To="toilet",Kind="backup",Condition="Borehole empty"}]};
        MeterData=Proxy.Make<IMeterData>((method,args)=>Complete(method,Meters));
        Readings=Proxy.Make<IMeterReadingData>((method,args)=>Complete(method,History));
        Waste=Proxy.Make<IWasteReadingData>((method,args)=>Complete(method,Array.Empty<WasteReadingDisplay>()));
        People=Proxy.Make<IDailyPeopleCountData>((method,args)=>Complete(method,Enumerable.Range(1,7).Select(i=>new DailyPeopleCount {Date=day.AddDays(i),Visitors=10,Employees=0})));
        Db=Proxy.Make<IDataAccess>((method,args)=> {
            var name=(string)args![0]!;
            if(name=="dbo.spWaterNetworkDocument_Get") return Complete(method,new[]{new WaterNetworkStore.Document{NetworkJson=JsonSerializer.Serialize(graph),Revision=1}});
            return Complete(method,Links.Select(p=>new ConsumptionService.Link {NodeId=p.Key,MeterId=p.Value}));
        });
    }
    public ConsumptionService Service()=>new(Readings,MeterData,new WaterNetworkStore(Db,new ConnectionStringData(),null!),Db,new ConnectionStringData(),Config);
    static object Complete(MethodInfo method,IEnumerable values)
    {
        var result=method.ReturnType.GetGenericArguments()[0];
        var item=result.GetGenericArguments()[0];
        var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(item))!;
        foreach(var value in values) list.Add(value);
        return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(result).Invoke(null,[list])!;
    }
}
public class Proxy:DispatchProxy
{
    public Func<MethodInfo,object?[]?,object?> Handler=null!;
    protected override object? Invoke(MethodInfo? method,object?[]? args)=>Handler(method!,args);
    public static T Make<T>(Func<MethodInfo,object?[]?,object?> handler) where T:class {var proxy=Create<T,Proxy>();((Proxy)(object)proxy).Handler=handler;return proxy;}
}
