// Local validation host only: synthetic users/data, no production configuration or connection.
using System.Collections;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DataLibrary.Data;
using DataLibrary.Db;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UtilityManagerProjects.Data;
using UtilityManagerProjects.Services;

var repository=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
var appRoot=Path.Combine(repository,"UtilityManagerProjects");
var builder=WebApplication.CreateBuilder(new WebApplicationOptions {Args=args,ContentRootPath=appRoot,WebRootPath=Path.Combine(appRoot,"wwwroot"),EnvironmentName="Development"});
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:5188");
var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
builder.Services.AddDbContext<ApplicationDbContext>(o=>o.UseSqlite(connection));
builder.Services.AddDefaultIdentity<IdentityUser>(o=>{o.User.RequireUniqueEmail=true;o.SignIn.RequireConfirmedAccount=false;}).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddUtilityPages();
builder.Services.AddAuthentication(o=>{o.DefaultAuthenticateScheme="Validation";o.DefaultChallengeScheme="Validation";o.DefaultForbidScheme="Validation";}).AddScheme<AuthenticationSchemeOptions,ValidationAuthentication>("Validation",_=>{});
builder.Services.AddScoped<UserAdministration>();builder.Services.AddScoped<ReferenceDataStore>();builder.Services.AddScoped<ConsumptionService>();builder.Services.AddScoped<WaterNetworkStore>();
builder.Services.AddSingleton(new ConnectionStringData());
builder.Configuration.AddInMemoryCollection(new Dictionary<string,string?> { ["WaterNetwork:MeterIds:main-tank"]="1",["WaterNetwork:MeterIds:fire-hydrant"]="2",["WaterNetwork:MeterIds:borehole-in"]="3",["WaterNetwork:MeterIds:garage-roof"]="4" });
var sample=new SampleData();
builder.Services.AddSingleton(sample.Data<IMeterData>());builder.Services.AddSingleton(sample.Data<IMeterReadingData>());builder.Services.AddSingleton(sample.Data<IWasteReadingData>());builder.Services.AddSingleton(sample.Data<IDailyPeopleCountData>());builder.Services.AddSingleton(sample.Data<IAreaData>());builder.Services.AddSingleton(sample.Data<IStationData>());builder.Services.AddSingleton(sample.Data<IEmployeeData>());builder.Services.AddSingleton(sample.Data<IDepartmentData>());builder.Services.AddSingleton(sample.Data<IWasteTypeData>());builder.Services.AddSingleton(sample.Data<IDataAccess>());
var app=builder.Build();app.UseStaticFiles();app.UseRouting();app.UseAuthentication();app.UseAuthorization();app.MapRazorPages();
void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
using(var scope=app.Services.CreateScope()) {
 var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();await db.Database.EnsureCreatedAsync();
 await db.Database.ExecuteSqlRawAsync("ATTACH DATABASE ':memory:' AS dbo");
 await db.Database.ExecuteSqlRawAsync("CREATE TABLE dbo.Employees(Id INTEGER PRIMARY KEY,Email TEXT NOT NULL,UserId TEXT NOT NULL,IsActive INTEGER NOT NULL)");
 var admin=scope.ServiceProvider.GetRequiredService<UserAdministration>();var users=scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
 await roles.CreateAsync(new IdentityRole("Admin"));await roles.CreateAsync(new IdentityRole("Reader"));
 Check((await admin.Create("admin@example.test","ValidPass1!")).Succeeded,"create Admin");
 var owner=(await users.FindByEmailAsync("admin@example.test"))!;await users.AddToRoleAsync(owner,"Admin");ValidationAuthentication.AdminId=owner.Id;
 var actor=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,owner.Id),new Claim(ClaimTypes.Role,"Admin")],"Validation"));
 Check((await admin.Create("reader@example.test","ValidPass1!")).Succeeded,"create user");
 var reader=(await users.FindByEmailAsync("reader@example.test"))!;
 Check(!(await admin.Create("reader@example.test","ValidPass1!")).Succeeded,"duplicate email rejected");
 Check((await admin.Update(reader.Id,"updated@example.test")).Succeeded,"edit user");
 using(var cmd=connection.CreateCommand()) {cmd.CommandText="SELECT Email FROM dbo.Employees WHERE UserId=$id";cmd.Parameters.AddWithValue("$id",reader.Id);Check((string?)await cmd.ExecuteScalarAsync()=="updated@example.test","employee email follows Identity edit");}
 Check(!(await admin.SetRoles(owner.Id,[],actor)).Succeeded,"self demotion rejected");
 Check(!(await admin.SetRoles(owner.Id,[],new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"other")])))).Succeeded,"last Admin demotion rejected");
 Check(!(await admin.Delete(owner.Id,actor)).Succeeded,"self deletion rejected");
 Check(!(await admin.Delete(owner.Id,new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"other")])))).Succeeded,"last Admin deletion rejected");
 Check(!(await admin.SetRoles(reader.Id,["Missing"],actor)).Succeeded,"unknown role rejected");
 Check((await admin.SetRoles(reader.Id,["Reader"],actor)).Succeeded,"assign role");
 Check((await users.IsInRoleAsync(reader,"Reader")),"role persisted");
 Check((await admin.Delete(reader.Id,actor)).Succeeded,"delete user");
 Check(await users.FindByIdAsync(reader.Id) is null,"Identity account removed");
 using(var cmd=connection.CreateCommand()) {cmd.CommandText="SELECT IsActive FROM dbo.Employees WHERE UserId=$id";cmd.Parameters.AddWithValue("$id",reader.Id);Check(Convert.ToInt32(await cmd.ExecuteScalarAsync())==0,"audit employee retained inactive");}
 // Force an employee write failure: account creation must roll back with it.
 await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER dbo.FailEmployee BEFORE INSERT ON Employees WHEN NEW.Email='fail@example.test' BEGIN SELECT RAISE(ABORT,'injected failure'); END");
 try {await admin.Create("fail@example.test","ValidPass1!");throw new Exception("failure was not injected");} catch(SqliteException) {}
 db.ChangeTracker.Clear();Check(await users.FindByEmailAsync("fail@example.test") is null,"employee failure rolls back Identity account");
 Console.WriteLine("PASS: real Identity create/edit/delete, duplicate emails, audit retention, role validation, self/last-Admin guards and transactional rollback (SQLite test database).");
}
await app.StartAsync();
using(var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false,UseProxy=false}){BaseAddress=new Uri("http://127.0.0.1:5188")}) {
 var protectedPaths=new[]{"/Admin/UserManagement","/Admin/UserManagement/Edit","/Admin/UserManagement/EditRoles","/Admin/UserRoles/Assign","/Admin/Roles","/Area/AreaIndex","/Area/Create","/Stations","/Stations/Edit","/MeterAreas/Create","/Identity/Account/Register"};
 foreach(var path in protectedPaths) {
  var anonymous=await client.GetAsync(path);Check(anonymous.StatusCode==HttpStatusCode.Unauthorized || (anonymous.StatusCode==HttpStatusCode.Redirect && anonymous.Headers.Location?.OriginalString.Contains("/Account/Login")==true),"anonymous blocked: "+path+" status="+anonymous.StatusCode+" location="+anonymous.Headers.Location);
  client.DefaultRequestHeaders.Add("X-Validation-Role","Reader");
  Check((await client.GetAsync(path)).StatusCode==HttpStatusCode.Forbidden,"non-Admin GET blocked: "+path);
  Check((await client.PostAsync(path,new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.Forbidden,"non-Admin POST blocked: "+path);
  client.DefaultRequestHeaders.Remove("X-Validation-Role");
 }
 client.DefaultRequestHeaders.Add("X-Validation-Role","Reader");
 foreach(var path in new[]{"/Dashboard/Dashboard","/MeterReadings/List","/WasteReadings/List"}) Check((await client.GetAsync(path)).StatusCode==HttpStatusCode.OK,"Reader can access operational overview: "+path);
 client.DefaultRequestHeaders.Remove("X-Validation-Role");
 client.DefaultRequestHeaders.Add("X-Validation-Role","Admin");
 foreach(var path in new[]{"/Admin/UserManagement","/Admin/UserManagement/Edit","/Area/AreaIndex","/Area/Create","/Stations","/Stations/Edit","/Dashboard/Dashboard?DateFilter=Last7","/MeterReadings/List","/Environmental","/Index"}) {
  var response=await client.GetAsync(path);if(response.StatusCode!=HttpStatusCode.OK) {Console.WriteLine(await response.Content.ReadAsStringAsync());throw new Exception("Admin page failed: "+path+" "+response.StatusCode);}
 }
 Check((await client.PostAsync("/Stations?handler=Delete",new FormUrlEncodedContent(new Dictionary<string,string>{{"id","1"}}))).StatusCode==HttpStatusCode.BadRequest,"Admin POST without antiforgery rejected");
 Console.WriteLine("PASS: anonymous and non-Admin GET/POST guards, authorized page rendering and antiforgery enforcement using actual Razor endpoints and shared access rules.");
}
if(args.Contains("--serve")) {Console.WriteLine("Validation UI ready at http://127.0.0.1:5188 (synthetic data)");await app.WaitForShutdownAsync();} else await app.StopAsync();

public sealed class ValidationAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
 public static string AdminId="";
 protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
  var role=Request.Headers["X-Validation-Role"].FirstOrDefault();
  if(role is null) return Task.FromResult(AuthenticateResult.NoResult());
  var principal=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,AdminId),new Claim(ClaimTypes.Name,"admin@example.test"),new Claim(ClaimTypes.Role,role)],Scheme.Name));
  return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal,Scheme.Name)));
 }
}
public sealed class SampleData
{
 readonly List<Meter> meters=Enumerable.Range(1,5).Select(i=>new Meter{Id=i,MeterName=new[]{"Main Meter","Fire Hydrant","Borehole Incoming","Garage Roof","Electricity"}[i-1],MeterType=i==5?MeterType.Electricity:MeterType.Water,IsActive=true,DepartmentName="Operations",AreaName="Lower station"}).ToList();
 public T Data<T>() where T:class => Proxy.Make<T>((method,args)=>{
  var resultType=method.ReturnType.GetGenericArguments()[0];
  if(!resultType.IsGenericType) throw new NotSupportedException(method.Name);
  var itemType=resultType.GetGenericArguments()[0];var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
  IEnumerable values=itemType==typeof(Meter)?meters:itemType==typeof(MeterReading)?meters.SelectMany(m=>Enumerable.Range(0,5).Select(i=>new MeterReading{Id=m.Id*10+i,MeterId=m.Id,MeterName=m.MeterName,MeterType=m.MeterType,DepartmentName=m.DepartmentName,AreaName=m.AreaName,ReadingDate=DateTime.Today.AddDays(-28+7*i),CurrentReading=1000+700*i,PreviousReading=i==0?1000:1000+700*(i-1),IsBaseline=i==0,Usage=i==0?0:700})):itemType==typeof(ReferenceDataStore.Row)?new[]{new ReferenceDataStore.Row{Id=1,Name="Lower station",StationName="Lower",StationId=1,IsActive=true}}:Array.Empty<object>();
  foreach(var item in values) list.Add(item);
  return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null,[list]);
 });
}
public class Proxy:DispatchProxy
{
 public Func<MethodInfo,object?[]?,object?> Handler=null!;
 protected override object? Invoke(MethodInfo? method,object?[]? args)=>Handler(method!,args);
 public static T Make<T>(Func<MethodInfo,object?[]?,object?> handler) where T:class {var proxy=Create<T,Proxy>();((Proxy)(object)proxy).Handler=handler;return proxy;}
}
