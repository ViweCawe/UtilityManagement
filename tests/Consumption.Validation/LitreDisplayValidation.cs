using DataLibrary.Models;
using UtilityManagerProjects.Models;
public static class LitreDisplayValidation
{
 static void Equal<T>(T expected,T actual,string message){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"{message}: expected {expected}, got {actual}");}
 public static void Run()
 {
  var day=new DateTime(2026,1,1);
  var reading=new MeterReading{Id=2,MeterId=1,MeterType=MeterType.Water,Unit="m³",CurrentReading=806,PreviousReading=800,Usage=6,ReadingDate=day.AddDays(7)};
  Equal(806000m,reading.DisplayCurrentReading,"806 m³ counter displays 806000 L");Equal(800000m,reading.DisplayPreviousReading,"previous counter uses same scale");Equal(6000m,reading.DisplayUsage,"6 m³ interval displays 6000 L");
  Equal("L",reading.DisplayUnit,"normalized display has an explicit litre unit");Equal(806m,reading.CurrentReading,"display conversion never changes captured counter");Equal("m³",reading.Unit,"original physical unit survives");
  var baseline=new MeterReading{Id=1,MeterId=1,MeterType=MeterType.Water,Unit="m³",CurrentReading=800,ReadingDate=day};
  var allocated=IntervalConsumption.Allocate([baseline,reading],day.AddDays(1),day.AddDays(7));Equal(6000m,allocated.Sum(r=>r.Usage),"allocator converts raw differences once, independently of the display");
  var copied=reading.ForReporting(10);Equal(806000m,copied.DisplayCurrentReading,"replacement reporting does not multiply a converted counter again");
  reading.CurrentReading=806.123456m;Equal(806123.456m,reading.DisplayCurrentReading,"red-dial decimal precision survives conversion");
  reading.Unit="kL";Equal(806123.456m,reading.DisplayCurrentReading,"kilolitres use the same litre conversion");reading.Unit="L";Equal(806.123456m,reading.DisplayCurrentReading,"litres are never multiplied again");
  reading.MeterType=MeterType.Electricity;reading.Unit="kWh";Equal(806.123456m,reading.DisplayCurrentReading,"electricity remains kWh");Equal("kWh",reading.DisplayUnit,"electricity label remains unchanged");
  var latest=new MeterDailyAverageRow{LatestCounter=806,CounterUnit="m³",MeterType=MeterType.Water};Equal(806000m,latest.DisplayLatestCounter,"dashboard latest counter agrees with reading pages");
  latest.CounterUnit="m²";Equal("m²",latest.CounterDisplayUnit,"unsupported legacy units are never silently relabelled as litres");
  var transfer=new WaterOutletMovement{OpeningCounter=800,ClosingCounter=806,CounterUnit="m³",Volume=6000};Equal(806000m,transfer.DisplayClosingCounter,"outlet capture table agrees");Equal(6000m,transfer.Volume,"normalized transfer volume is not scaled twice");
  Console.WriteLine("PASS: 806 m³ = 806000 L, raw counter preservation, fractional/kL conversion, unchanged L/kWh, latest-counter agreement and no double conversion.");
 }
}
