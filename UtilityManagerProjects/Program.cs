using DataLibrary.Data;
using DataLibrary.Db;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UtilityManagerProjects.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("Default") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddSingleton(new ConnectionStringData
{
    SqlConnectionName = "Default"
});
builder.Services.AddSingleton<IDataAccess, SqlDb>();
builder.Services.AddSingleton<IAreaData, AreaData>();
builder.Services.AddSingleton<IMeterData, MeterData>();
builder.Services.AddSingleton<IStationData, StationData>();
builder.Services.AddSingleton<IMeterReadingData, MeterReadingData>();
builder.Services.AddSingleton<IEmployeeData , EmployeeData>();
builder.Services.AddSingleton<IWasteReadingData, WasteReadingData>();
builder.Services.AddSingleton<IWasteTypeData, WasteTypeData>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
