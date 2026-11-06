using Microsoft.SqlServer.TransactSql.ScriptDom;
var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
var folder=Path.Combine(root,"UtilityManagementApp");
var files=new[]{"Scripts/UpgradeMeterDirectionSave.sql","Scripts/UpgradeMeterLifecycle.sql","dbo/Tables/Meters.sql","dbo/Tables/MeterReadings.sql","dbo/Tables/MeterReadingImportBatches.sql","dbo/Indexes/MeterReplacementIndex.sql"};
foreach(var file in files){using var input=File.OpenText(Path.Combine(folder,file));new TSql160Parser(true).Parse(input,out var errors);if(errors.Count>0)throw new Exception(file+": "+string.Join("; ",errors.Select(e=>$"line {e.Line}: {e.Message}")));}
var meterSchema=File.ReadAllText(Path.Combine(folder,"dbo/Tables/Meters.sql"));
foreach(var column in new[]{"SerialNumber","FlowDirection","ReplacesMeterId","InstalledOn","RetiredOn"})if(!meterSchema.Contains("["+column+"]"))throw new Exception("Database project is missing lifecycle column "+column);
var migration=File.ReadAllText(Path.Combine(folder,"Scripts/UpgradeMeterLifecycle.sql"));
foreach(var file in new[]{"dbo/Views/vwMeterReadingIntervals.sql","dbo/StoredProcedures/spMeters_All.sql","dbo/StoredProcedures/spMeterS_GetById.sql","dbo/StoredProcedures/spMeters_Insert.sql","dbo/StoredProcedures/spMeters_UpdateDetails.sql","dbo/StoredProcedures/spMeters_Delete.sql","dbo/StoredProcedures/spMeterReadings_All.sql","dbo/StoredProcedures/spMeterReadings_GetByDateRange.sql","dbo/StoredProcedures/spMeterReadings_Insert.sql","dbo/StoredProcedures/spMeterReadings_Update.sql","dbo/StoredProcedures/spMeterReadings_Import.sql"}) {
 var expected=File.ReadAllText(Path.Combine(folder,file)).Trim().Replace("CREATE PROCEDURE","CREATE OR ALTER PROCEDURE").Replace("CREATE VIEW","CREATE OR ALTER VIEW");
 if(!migration.Contains(expected))throw new Exception("Migration is out of sync with "+file);
}
Console.WriteLine("PASS: SQL Server parser validates migration/schema and all changed procedure/view definitions match the upgrade script. Execution against SQL Server is a separate deployment check.");

var directionPatch=File.ReadAllText(Path.Combine(folder,"Scripts/UpgradeMeterDirectionSave.sql"));
var updateProcedure=File.ReadAllText(Path.Combine(folder,"dbo/StoredProcedures/spMeters_UpdateDetails.sql")).Trim().Replace("CREATE PROCEDURE","CREATE OR ALTER PROCEDURE");
if(!directionPatch.Contains(updateProcedure))throw new Exception("Direction hotfix is out of sync with the update procedure.");
