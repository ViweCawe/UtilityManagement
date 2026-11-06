using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using DataLibrary.Models;
namespace UtilityManagerProjects.Models;

public sealed record ImportReading(int MeterId,DateTime ReadingDate,decimal Counter,string Unit);
public sealed record ImportCheck(ImportReading Reading,string Status,string Detail);
public static class MeterReadingImport
{
 public const int MaxRows=10000;
 public static List<ImportReading> Parse(Stream stream,string fileName)
 {
  var rows=Path.GetExtension(fileName).ToLowerInvariant() switch {".csv"=>Csv(new StreamReader(stream,Encoding.UTF8,true).ReadToEnd()),".xlsx"=>Xlsx(stream),_=>throw new InvalidDataException("Upload .csv or .xlsx using the supplied columns.")};
  if(rows.Count<2||rows.Count>MaxRows+1)throw new InvalidDataException("Use between 1 and 10,000 reading rows.");
  var header=rows[0].Select(v=>v.Trim()).ToArray();var expected=new[]{"MeterId","ReadingDate","Counter","Unit"};
  if(!header.SequenceEqual(expected,StringComparer.OrdinalIgnoreCase))throw new InvalidDataException("Use columns MeterId, ReadingDate, Counter, Unit. Convert the workbook’s wide meter columns into one row per physical meter capture; range labels such as 1–7 Jan need an actual capture date.");
  return rows.Skip(1).Select((r,i)=>{
   if(r.Length!=4||!int.TryParse(r[0],out var id)||id<=0||!DateTime.TryParseExact(r[1],new[]{"yyyy-MM-dd","yyyy-MM-ddTHH:mm:ss","yyyy-MM-dd HH:mm:ss"},CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)||!decimal.TryParse(r[2],NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var counter))throw new InvalidDataException($"Row {i+2}: use a meter ID, ISO capture date/time and a plain decimal counter (no thousands separators).");
   return new ImportReading(id,date,counter,string.IsNullOrWhiteSpace(r[3])?"":MeterUnits.Normalize(r[3],MeterType.Water));
  }).ToList();
 }
 static List<string[]> Csv(string input)
 {
  var rows=new List<string[]>();var cells=new List<string>();var cell=new StringBuilder();bool quoted=false;
  for(int i=0;i<input.Length;i++){
   var c=input[i];if(c=='"'){if(quoted&&i+1<input.Length&&input[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}
   else if(c==','&&!quoted){cells.Add(cell.ToString());cell.Clear();}
   else if((c=='\n'||c=='\r')&&!quoted){if(c=='\r'&&i+1<input.Length&&input[i+1]=='\n')i++;cells.Add(cell.ToString());cell.Clear();if(cells.Any(s=>s.Length>0))rows.Add(cells.ToArray());cells.Clear();}
   else cell.Append(c);
   if(rows.Count>MaxRows+1)throw new InvalidDataException("Too many rows.");
  }
  if(quoted)throw new InvalidDataException("Unclosed CSV quote.");
  if(cell.Length>0||cells.Count>0){cells.Add(cell.ToString());rows.Add(cells.ToArray());}return rows;
 }
 static List<string[]> Xlsx(Stream stream)
 {
  using var zip=new ZipArchive(stream,ZipArchiveMode.Read,true);
  if(zip.Entries.Count>2000||zip.Entries.Sum(e=>e.Length)>30_000_000)throw new InvalidDataException("Workbook is too large. Export the reading table alone.");
  XDocument Read(string path){var entry=zip.GetEntry(path)??throw new InvalidDataException("Invalid workbook structure.");using var s=entry.Open();return XDocument.Load(s);}
  XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
  var workbook=Read("xl/workbook.xml");var sheet=workbook.Descendants(ns+"sheet").FirstOrDefault()??throw new InvalidDataException("Workbook has no sheet.");
  var relationship=(string?)sheet.Attribute(XName.Get("id","http://schemas.openxmlformats.org/officeDocument/2006/relationships"));var rel=Read("xl/_rels/workbook.xml.rels").Root!.Elements().SingleOrDefault(e=>(string?)e.Attribute("Id")==relationship);
  var target=(string?)rel?.Attribute("Target")??"";if((string?)rel?.Attribute("TargetMode")=="External"||target.Contains(".."))throw new InvalidDataException("Unsupported worksheet relationship.");
  var sheetPath=target.StartsWith('/')?target.TrimStart('/') : "xl/"+target;
  var strings=zip.GetEntry("xl/sharedStrings.xml") is null?[]:Read("xl/sharedStrings.xml").Descendants(ns+"si").Select(si=>string.Concat(si.Descendants(ns+"t").Select(t=>t.Value))).ToList();
  var rows=new List<string[]>();bool use1904=(string?)workbook.Root?.Element(ns+"workbookPr")?.Attribute("date1904") is "1" or "true";
  foreach(var row in Read(sheetPath).Descendants(ns+"row")){
   var cells=new string[4];Array.Fill(cells,"");
   foreach(var c in row.Elements(ns+"c")){
    if(c.Element(ns+"f") is not null)throw new InvalidDataException("Import actual counters, not formulas or cached formula results.");
    var address=(string?)c.Attribute("r")??"";var column=new string(address.TakeWhile(char.IsLetter).ToArray());if(column.Length!=1||column[0]<'A'||column[0]>'D'){if(c.Element(ns+"v") is not null||c.Element(ns+"is") is not null)throw new InvalidDataException("Use only the four supplied columns.");continue;}
    var index=column[0]-'A';var value=c.Element(ns+"v")?.Value??string.Concat(c.Descendants(ns+"t").Select(t=>t.Value));var type=(string?)c.Attribute("t");
    if(type=="s"){if(!int.TryParse(value,out var si)||si<0||si>=strings.Count)throw new InvalidDataException("Invalid shared string.");value=strings[si];}
    if(index==1&&rows.Count>0&&(type is null or "n")&&double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var serial)){try{value=(use1904?new DateTime(1904,1,1).AddDays(serial):DateTime.FromOADate(serial)).ToString("yyyy-MM-ddTHH:mm:ss",CultureInfo.InvariantCulture);}catch(ArgumentException){throw new InvalidDataException("Invalid Excel capture date.");}}
    cells[index]=value;
   }
   if(cells.Any(c=>c.Length>0))rows.Add(cells);if(rows.Count>MaxRows+1)throw new InvalidDataException("Too many rows.");
  }return rows;
 }
 public static List<ImportCheck> Check(List<ImportReading> rows,List<Meter> meters,List<MeterReading> history)
 {
  var known=meters.ToDictionary(m=>m.Id);var duplicates=rows.GroupBy(r=>(r.MeterId,r.ReadingDate)).Where(g=>g.Count()>1).Select(g=>g.Key).ToHashSet();
  var existingByMeter=history.Where(r=>!r.IsDeleted).GroupBy(r=>r.MeterId).ToDictionary(g=>g.Key,g=>g.ToList());
  var chains=rows.GroupBy(r=>r.MeterId).ToDictionary(g=>g.Key,g=>existingByMeter.GetValueOrDefault(g.Key,[]).Select(r=>(Date:r.ReadingDate,Counter:r.CurrentReading)).Concat(g.Select(r=>(Date:r.ReadingDate,Counter:r.Counter))).Distinct().OrderBy(r=>r.Date).ToList());
  return rows.Select(row=>{
   string? error=null;var status="New";
   if(!known.TryGetValue(row.MeterId,out var meter))error="Unknown physical meter ID.";
   else if(string.IsNullOrWhiteSpace(row.Unit)||!MeterUnits.Valid(row.Unit,meter.MeterType)||MeterUnits.Normalize(row.Unit,meter.MeterType)!=meter.Unit)error="Unit must match this physical counter; do not import converted totals as counter readings.";
   else if(!MeterUnits.ValidCounter(row.Counter))error="Counter must be nonnegative, at most 13 integer digits and six decimal places.";
   else if(row.ReadingDate<new DateTime(1753,1,1)||row.ReadingDate>DateTime.Now||meter.InstalledOn.HasValue&&row.ReadingDate<meter.InstalledOn||meter.RetiredOn.HasValue&&row.ReadingDate>meter.RetiredOn)error="Capture is outside this physical meter’s service dates or in the future.";
   else if(!meter.IsActive&&!meter.RetiredOn.HasValue)error="Inactive meter needs a confirmed retirement/cutover date before historical import.";
   if(duplicates.Contains((row.MeterId,row.ReadingDate)))error="Duplicate meter/time in this file.";
   var existing=existingByMeter.GetValueOrDefault(row.MeterId,[]);var exact=existing.FirstOrDefault(r=>r.ReadingDate==row.ReadingDate);
   if(exact is not null){if(exact.CurrentReading==row.Counter)status="Existing";else error="A different counter already exists at this time. Review it instead of overwriting.";}
   if(error is null){var chain=chains[row.MeterId];var before=chain.LastOrDefault(r=>r.Date<row.ReadingDate);var after=chain.FirstOrDefault(r=>r.Date>row.ReadingDate);if(before.Date!=default&&row.Counter<before.Counter||after.Date!=default&&row.Counter>after.Counter)error="Counter decreases within the same physical meter. Check the date or assign the replaced meter ID.";}
   return new ImportCheck(row,error is null?status:"Error",error??(status=="Existing"?"Already captured; skipped.":"Ready. First capture on each physical meter is a baseline."));
  }).ToList();
 }
}
