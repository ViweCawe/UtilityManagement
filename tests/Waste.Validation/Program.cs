using System.Reflection;
using System.Security.Claims;
using DataLibrary.Data;
using DataLibrary.Db;
using DataLibrary.Models;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UtilityManagerProjects.Pages.WasteReadings;
static void Equal<T>(T expected,T actual,string why){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"{why}: expected {expected}, got {actual}");}
static void Attach(PageModel page){var http=new DefaultHttpContext();http.User=new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"operator")],"test"));page.PageContext=new(){HttpContext=http,ViewData=new ViewDataDictionary(new EmptyModelMetadataProvider(),new ModelStateDictionary())};page.TempData=new TempDataDictionary(http,new MemoryTemp());}
var date=new DateTime(2025,1,10);
var types=Proxy.Make<IWasteTypeData>((m,a)=>Task.FromResult<List<WasteType>>([new(){Id=1,WasteTypeName="Recycling",IsActive=true},new(){Id=2,IsActive=false}]));
var users=new UserManager<IdentityUser>(Proxy.Make<IUserStore<IdentityUser>>((m,a)=>null),Options.Create(new IdentityOptions()),new PasswordHasher<IdentityUser>(),[],[],new UpperInvariantLookupNormalizer(),new IdentityErrorDescriber(),new ServiceCollection().BuildServiceProvider(),NullLogger<UserManager<IdentityUser>>.Instance);
var employees=Proxy.Make<IEmployeeData>((m,a)=>Task.FromResult<Employee?>(new(){Id=7,IsActive=true}));
var writes=new List<(string Name,object Parameters)>();
var database=Proxy.Make<IDataAccess>((m,a)=>{writes.Add(((string)a![0]!,a[1]!));if(a[1] is DynamicParameters dp)dp.Add("Id",123);return Task.FromResult(1);});
var data=new WasteReadingData(database,new ConnectionStringData(),types);
await data.CreateWasteReading(new(){WasteTypeId=1,WasteAmount=12.25m,ReadingDate=date,CreatedBy=7,CreatedAt=DateTime.Today});
Equal(date,((DynamicParameters)writes.Last().Parameters).Get<DateTime>("RecordedAt"),"data layer preserves backdated event date instead of CreatedAt");
await data.UpdateWasteReadings(123,14.25m,"corrected",7,date.AddDays(1));
Equal(date.AddDays(1),(DateTime?)writes.Last().Parameters.GetType().GetProperty("ReadingDate")!.GetValue(writes.Last().Parameters),"edit transmits collection date");
await data.DeleteWasteReading(123,7);
Equal("dbo.spWasteReadings_Delete",writes.Last().Name,"delete uses audited soft-delete procedure");
Equal(7,(int)writes.Last().Parameters.GetType().GetProperty("UpdatedBy")!.GetValue(writes.Last().Parameters)!,"deletion carries employee identity");
var existingRecord=new WasteReading{Id=123,WasteTypeId=1,WasteAmount=12.25m,ReadingDate=date};
DateTime? savedDate=null;int deletedBy=0;
var rows=new List<WasteReadingDisplay>{new(){Id=1,WasteTypeId=1,WasteTypeName="Recycling",WasteCategory="Recycling",WasteMaterial="Paper",WasteAmount=10,ReadingDate=date},new(){Id=2,WasteTypeId=1,WasteTypeName="Recycling",WasteCategory="Recycling",WasteMaterial="Paper",WasteAmount=20,ReadingDate=date},new(){Id=3,WasteTypeId=1,WasteTypeName="Recycling",WasteCategory="Recycling",WasteMaterial="Paper",WasteAmount=5,ReadingDate=date.AddDays(-1)},new(){Id=4,WasteTypeId=2,WasteTypeName="General",WasteCategory="Landfill",WasteAmount=500,ReadingDate=date.AddDays(-1)}};
var readings=Proxy.Make<IWasteReadingData>((m,a)=>m.Name switch{
 "GetWasteReadingDisplay"=>Task.FromResult<IEnumerable<WasteReadingDisplay>>(rows),
 "GetWasteReadingById"=>Task.FromResult<WasteReading?>(existingRecord),
 "CreateWasteReading"=>Save((WasteReading)a![0]!),
 "DeleteWasteReading"=>Delete((int)a![1]!),
 "UpdateWasteReadings"=>Update((DateTime?)a![4]),
 _=>throw new Exception(m.Name)});
Task<int> Save(WasteReading r){savedDate=r.ReadingDate;Equal(7,r.CreatedBy,"page binds employee from authenticated account");return Task.FromResult(123);}
Task<int> Delete(int employee){deletedBy=employee;return Task.FromResult(1);}
Task<int> Update(DateTime? value){savedDate=value;return Task.FromResult(1);}
var create=new CreateModel(readings,types,employees,users){WasteRecord=new(){WasteTypeId=1,WasteAmount=12.25m,ReadingDate=date}};Attach(create);
Equal(true,await create.OnPostAsync() is RedirectToPageResult,"valid backdated create succeeds");Equal(date,savedDate,"page preserves selected date");
var invalid=new CreateModel(readings,types,employees,users){WasteRecord=new(){WasteTypeId=2,WasteAmount=1,ReadingDate=date}};Attach(invalid);await invalid.OnPostAsync();Equal(false,invalid.ModelState.IsValid,"inactive waste type rejected");Equal(1,invalid.WasteTypeList.Count,"dropdown repopulates with active types on error");
var edit=new DisplayModel(readings,types,employees,users){Id=123,WasteRecordUpdate=new(){Id=123,WasteAmount=14,ReadingDate=date.AddDays(1)}};Attach(edit);await edit.OnPostAsync();Equal(date.AddDays(1),savedDate,"edit page preserves corrected event date");
var delete=new DeleteModel(readings,employees,users){Id=123};Attach(delete);Equal(true,await delete.OnPostAsync() is RedirectToPageResult,"delete route completes");Equal(7,deletedBy,"delete page uses authenticated employee");existingRecord.IsDeleted=true;Equal(true,await delete.OnGetAsync() is NotFoundResult,"deleted records cannot be reopened");
var list=new ListModel(readings){DateFilter="Custom",StartDate=date,EndDate=date,SearchTerm="Recycling"};Attach(list);await list.OnGet();Equal(2,list.TotalReadings,"multiple collections of same type remain independent events");Equal(30m,list.TotalWasteGenerated,"waste is additive event weight, not counter differences");Equal(5m,list.PreviousWasteGenerated,"search applies consistently to previous period");Equal(500d,list.WasteGrowthPercent,"like-for-like filtered growth");
rows.Add(new(){Id=5,WasteTypeId=3,WasteTypeName="Mixed",WasteCategory="Recycling / Landfill",WasteAmount=7,ReadingDate=date});
for(int i=0;i<7;i++)rows.Add(new(){Id=10+i,WasteTypeId=10+i,WasteCategory="Category "+i,WasteMaterial="Material "+i,WasteAmount=1,ReadingDate=date});
var all=new ListModel(readings){DateFilter="Custom",StartDate=date,EndDate=date};Attach(all);await all.OnGet();Equal(all.TotalWasteGenerated,all.RecyclingWaste+all.LandfillWaste+all.OtherWaste,"exclusive disposition totals conserve all event weight");Equal(all.TotalWasteGenerated,System.Text.Json.JsonSerializer.Deserialize<decimal[]>(all.CategoryDataJson)!.Sum(),"chart remainder retains categories after top five");
var noPrior=new ListModel(readings){DateFilter="Custom",StartDate=date.AddDays(-1),EndDate=date.AddDays(-1),SearchTerm="Recycling"};Attach(noPrior);await noPrior.OnGet();Equal(false,noPrior.WasteGrowthAvailable,"no fabricated percentage when previous amount is zero");
Console.WriteLine("PASS: waste data date mapping, audit deletion, authenticated create/edit/delete pages, invalid dropdown retention, event totals, filter consistency, chart conservation and growth availability.");
public class Proxy:DispatchProxy{public Func<MethodInfo,object?[]?,object?> Handler=null!;protected override object? Invoke(MethodInfo? m,object?[]? a)=>Handler(m!,a);public static T Make<T>(Func<MethodInfo,object?[]?,object?> h)where T:class{var p=Create<T,Proxy>();((Proxy)(object)p).Handler=h;return p;}}
public class MemoryTemp:ITempDataProvider{public IDictionary<string,object> LoadTempData(HttpContext c)=>new Dictionary<string,object>();public void SaveTempData(HttpContext c,IDictionary<string,object> values){}}
