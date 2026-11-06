using DataLibrary.Models;
using UtilityManagerProjects.Models;
using System.Text;
using System.IO.Compression;

public static class MeterLifecycleValidation
{
 static void Equal<T>(T expected,T actual,string message){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"{message}: expected {expected}, got {actual}");}
 static void Reject(Action action,string message){try{action();}catch(InvalidDataException){return;}throw new Exception(message);}
 public static void Run()
 {
  var day=new DateTime(2021,1,1);var fixture=new Fixture(day);var old=fixture.Meters[0];old.IsActive=false;old.Unit="m³";old.RetiredOn=day.AddDays(7);
  fixture.History.Single(r=>r.Id==2).CurrentReading=1000.7m;
  fixture.Meters.Add(new(){Id=10,MeterName="Replacement Main",MeterType=MeterType.Water,Unit="L",DepartmentName=old.DepartmentName,ReplacesMeterId=1,InstalledOn=day.AddDays(7),IsActive=true});
  fixture.History.Add(new(){Id=100,MeterId=10,MeterName="Replacement Main",MeterType=MeterType.Water,ReadingDate=day.AddDays(7),CurrentReading=200});
  fixture.History.Add(new(){Id=101,MeterId=10,MeterName="Replacement Main",MeterType=MeterType.Water,ReadingDate=day.AddDays(14),CurrentReading=700});
  var service=fixture.Service();service.LoadAsync().GetAwaiter().GetResult();
  Equal(1200m,decimal.Round(service.ForPeriod(day.AddDays(1),day.AddDays(14)).Water.Total,6),"five-year-old replaced meter m³ intervals join new L intervals without cross-counter subtraction");
  Equal(14,service.ForPeriod(day.AddDays(1),day.AddDays(14)).Water.CoveredDays,"replacement preserves covered days");
  Equal(700m,service.ForPeriod(day.AddDays(1),day.AddDays(7),1).Water.Total,"selecting the retired meter resolves its measurement-point chain");
  Equal(700m,service.FlowTraceForPeriod(day.AddDays(1),day.AddDays(7)).Points.Single(p=>p.ReportingRole=="main").Flow.Total,"station tracing uses the same replacement history");
  fixture.History.Single(r=>r.Id==100).ReadingDate=day.AddDays(9);
  var gapped=fixture.Service();gapped.LoadAsync().GetAwaiter().GetResult();
  Equal(12,gapped.ForPeriod(day.AddDays(1),day.AddDays(14)).Water.CoveredDays,"missing cutover captures stay as gaps, never interpolated between physical counters");
  fixture.History.Single(r=>r.Id==100).ReadingDate=day.AddDays(7);
  Equal(4,service.History.Count(r=>r.MeterId is 1 or 10),"raw captures retain physical meter IDs");
  Equal(1000m,MeterUnits.Factor("m3",MeterType.Water),"cubic units normalize to litres");Equal(false,MeterUnits.Valid("m²",MeterType.Water),"square metres are not water volume");
  var lineage=new MeterLineage(fixture.Meters);Equal(2,lineage.Family(1).Count,"chain visible from either physical meter");
  fixture.Meters.Add(new(){Id=11,ReplacesMeterId=1,InstalledOn=old.RetiredOn,MeterType=MeterType.Water});Reject(()=>new MeterLineage(fixture.Meters),"branching replacements rejected");fixture.Meters.RemoveAt(fixture.Meters.Count-1);
  using var csv=new MemoryStream(Encoding.UTF8.GetBytes("MeterId,ReadingDate,Counter,Unit\n1,2021-01-04,1000.3,m3\n10,2021-01-10,400,L\n"));
  var rows=MeterReadingImport.Parse(csv,"history.csv");var checks=MeterReadingImport.Check(rows,fixture.Meters,fixture.History);Equal(true,checks.All(c=>c.Status=="New"),"retired and active physical imports validate within cutover dates");
  Equal("Error",MeterReadingImport.Check([rows[0] with {ReadingDate=day.AddDays(10)}],fixture.Meters,fixture.History)[0].Status,"retired counter cannot be imported after cutover");
  Equal("Error",MeterReadingImport.Check([rows[1] with {Counter=100}],fixture.Meters,fixture.History)[0].Status,"decreasing physical counter rejected");
  Equal("Error",MeterReadingImport.Check([rows[1],rows[1]],fixture.Meters,fixture.History)[0].Status,"duplicate import rows rejected");
  Equal("Existing",MeterReadingImport.Check([new(10,day.AddDays(14),700,"L")],fixture.Meters,fixture.History)[0].Status,"identical existing counter skipped");
  Equal("Error",MeterReadingImport.Check([new(10,day.AddDays(14),701,"L")],fixture.Meters,fixture.History)[0].Status,"existing counter never overwritten by import");
  Equal("Error",MeterReadingImport.Check([rows[1] with {Unit="m²"}],fixture.Meters,fixture.History)[0].Status,"area-unit import rejected");
  using var bad=new MemoryStream(Encoding.UTF8.GetBytes("MeterId,ReadingDate,Counter,Unit\n10,1-7 Jan,100,L\n"));Reject(()=>MeterReadingImport.Parse(bad,"history.csv"),"weekly range cannot silently become capture date");
  using var xlsx=new MemoryStream();using(var zip=new ZipArchive(xlsx,ZipArchiveMode.Create,true)){
   void Entry(string path,string value){using var writer=new StreamWriter(zip.CreateEntry(path).Open());writer.Write(value);}
   Entry("xl/workbook.xml","<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet name='Readings' r:id='rId1'/></sheets></workbook>");
   Entry("xl/_rels/workbook.xml.rels","<Relationships><Relationship Id='rId1' Target='worksheets/sheet1.xml'/></Relationships>");
   Entry("xl/worksheets/sheet1.xml","<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData><row><c r='A1' t='inlineStr'><is><t>MeterId</t></is></c><c r='B1' t='inlineStr'><is><t>ReadingDate</t></is></c><c r='C1' t='inlineStr'><is><t>Counter</t></is></c><c r='D1' t='inlineStr'><is><t>Unit</t></is></c></row><row><c r='A2'><v>10</v></c><c r='B2' t='inlineStr'><is><t>2021-01-10</t></is></c><c r='C2'><v>400.125</v></c><c r='D2' t='inlineStr'><is><t>L</t></is></c></row></sheetData></worksheet>");
  }xlsx.Position=0;Equal(400.125m,MeterReadingImport.Parse(xlsx,"readings.xlsx")[0].Counter,"XLSX retains decimal counter precision");
  Console.WriteLine("PASS: five-year replacement history, mixed units, no cross-counter subtraction, station agreement, CSV/XLSX validation, original IDs, duplicate/conflict and service-date guards.");
 }
}
