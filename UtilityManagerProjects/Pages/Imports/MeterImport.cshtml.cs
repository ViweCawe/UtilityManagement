using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using DataLibrary.Data;
using DataLibrary.Db;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UtilityManagerProjects.Models;
namespace UtilityManagerProjects.Pages.Imports;
[RequestSizeLimit(12_000_000)]
public class MeterImportModel(IMeterData meters,IMeterReadingData readings,IEmployeeData employees,IDataAccess db,ConnectionStringData connection,IDataProtectionProvider protection,ILogger<MeterImportModel> logger) : PageModel
{
 [BindProperty] public IFormFile? Upload { get; set; }
 [BindProperty] public string? PreviewToken { get; set; }
 public List<ImportCheck> Checks { get; private set; }=[];
 public string Message { get; private set; }="";
 public sealed record Preview(string UserId,DateTime Expires,string FileName,string Hash,List<ImportReading> Rows);
 const string Purpose="MeterReadingImport.v1";
 public IActionResult OnGetTemplate()=>File(Encoding.UTF8.GetBytes("MeterId,ReadingDate,Counter,Unit\r\n"),"text/csv","meter-reading-template.csv");
 public async Task<IActionResult> OnPostPreviewAsync()
 {
  try {
   if(Upload is null||Upload.Length==0||Upload.Length>5_000_000)throw new InvalidDataException("Choose a CSV or XLSX file up to 5 MB.");
   using var content=new MemoryStream();await Upload.CopyToAsync(content);content.Position=0;
   var rows=MeterReadingImport.Parse(content,Upload.FileName);Checks=MeterReadingImport.Check(rows,await meters.GetMeters(),(await readings.GetAllMeterReading()).ToList());
   if(Checks.Any(c=>c.Status=="Error")){Message="Fix every error before importing. No readings were saved.";return Page();}
   var payload=new Preview(User.FindFirstValue(ClaimTypes.NameIdentifier)??"",DateTime.UtcNow.AddMinutes(15),Path.GetFileName(Upload.FileName)[..Math.Min(Path.GetFileName(Upload.FileName).Length,200)],Convert.ToHexString(SHA256.HashData(content.ToArray())),rows);
   PreviewToken=protection.CreateProtector(Purpose).Protect(JsonSerializer.Serialize(payload));Message="Review the physical meter IDs, capture dates and original units, then import. Existing identical captures are skipped.";
  } catch(Exception ex) when(ex is InvalidDataException or FormatException or System.Xml.XmlException or ArgumentException){Message=ex.Message;}
  return Page();
 }
 public async Task<IActionResult> OnPostConfirmAsync()
 {
  try {
   var payload=JsonSerializer.Deserialize<Preview>(protection.CreateProtector(Purpose).Unprotect(PreviewToken??""))??throw new InvalidDataException("Preview is invalid.");
   if(payload.Expires<DateTime.UtcNow||payload.UserId!=User.FindFirstValue(ClaimTypes.NameIdentifier))throw new InvalidDataException("Preview expired. Upload and review the file again.");
   Checks=MeterReadingImport.Check(payload.Rows,await meters.GetMeters(),(await readings.GetAllMeterReading()).ToList());
   if(Checks.Any(c=>c.Status=="Error"))throw new InvalidDataException("Data changed or validation failed. Review the errors and upload again.");
   var employee=await employees.GetEmployeeByUserId(payload.UserId);if(employee is null)throw new InvalidDataException("Your account needs a linked employee before importing.");
   var parameters=new DynamicParameters();parameters.Add("RowsJson",JsonSerializer.Serialize(payload.Rows));parameters.Add("EmployeeId",employee.Id);parameters.Add("FileName",payload.FileName);parameters.Add("FileHash",payload.Hash);parameters.Add("Inserted",dbType:System.Data.DbType.Int32,direction:System.Data.ParameterDirection.Output);
   await db.SaveData("dbo.spMeterReadings_Import",parameters,connection.SqlConnectionName);
   Message=$"Imported {parameters.Get<int>("Inserted")} captures. Existing captures were preserved.";PreviewToken=null;
  } catch(Exception ex) when(ex is InvalidDataException or CryptographicException or JsonException){Message=ex is CryptographicException?"Preview could not be verified. Upload again.":ex.Message;PreviewToken=null;}
  catch(Exception ex){logger.LogError(ex,"Meter reading import failed");Message="Import could not be completed. No partial batch was saved. Check the database upgrade and retry the preview.";PreviewToken=null;}
  return Page();
 }
}
