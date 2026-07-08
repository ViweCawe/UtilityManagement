using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UtilityManagerProjects.Pages.Dashboard
{
    public class DashboardModel : PageModel
    {
        private readonly IMeterReadingData readingData;
        private readonly IWasteReadingData wasteReading;

        public DashboardModel( IMeterReadingData readingData,IWasteReadingData wasteReading)
        {
            this.readingData = readingData;
            this.wasteReading = wasteReading;
        }


        public int WaterConsumption { get; set; }
        public int ElectricityConsumption { get; set; }
        public decimal WasteGenerated { get; set; }
        public IEnumerable<MeterReading> MeterReadings { get; set; }
        public IEnumerable<WasteReading> WasteReadings { get; set; }
        public List<MeterTypeKpi> MeterTypeKpis { get; set; } = new List<MeterTypeKpi>();

        public async Task OnGet()
        {

            //Get All the meter readings and waste readings from the database
            var meterReadingsTask =await readingData.GetAllMeterReading();
            var wasteReadingTask = await wasteReading.GetAllWasteReadings();


            //Calculate the total consumption for electricity, water, and waste
            ElectricityConsumption = meterReadingsTask
                .Where(x => x.MeterType == MeterType.Electricity)
                .Sum(x => x.Usage);

            //Get Waste Consumption Calculation 

            WaterConsumption = meterReadingsTask
                .Where(x => x.MeterType == MeterType.Water)
                .Sum(x => x.Usage);


            //Get The Amount of waste Generated 
            WasteGenerated = wasteReadingTask
                .Where(x => x.IsDeleted == false)
                .Sum(x => x.WasteAmount);



            var today = DateTime.Today;

            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7; 
            var startOfLastWeek = today.AddDays(-daysSinceMonday - 7);
            var endOfLastWeek = startOfLastWeek;
            
            var currentWeelyWater = meterReadingsTask
                .Where(x => x.MeterType == MeterType.Water && x.ReadingDate >= startOfLastWeek && x.ReadingDate <= endOfLastWeek)
                .Sum(x => x.Usage);

            var lastWeeksTotal = meterReadingsTask
                .Where(x => x.MeterType == MeterType.Water
                && x.ReadingDate >= startOfLastWeek
                && x.ReadingDate <= endOfLastWeek)
                .Sum(x => x.Usage);

            decimal waterPecentageChange = 0;
            decimal electricityPercentageChange = 0;
            decimal wastePercentageChange = 0;

            var currentWeeklyElectricity = meterReadingsTask
                .Where(x => x.MeterType == MeterType.Electricity
                            && x.ReadingDate >= startOfLastWeek
                            && x.ReadingDate <= endOfLastWeek)
                .Sum(x => x.Usage);

            var lastWeeksElectricityTotal = meterReadingsTask
                .Where(x => x.MeterType == MeterType.Electricity
                            && x.ReadingDate >= startOfLastWeek
                            && x.ReadingDate <= endOfLastWeek)
                .Sum(x => x.Usage);



            var percentage = 0;
            var wasteCurrentWeek = wasteReadingTask
                .Where(x => x.ReadingDate >= startOfLastWeek
                            && x.ReadingDate <= endOfLastWeek)
                .Sum(x => x.WasteAmount);   


            if (lastWeeksTotal > 0)
            {
                waterPecentageChange = ((decimal)currentWeelyWater - lastWeeksTotal) / lastWeeksTotal * 100;
                electricityPercentageChange = ((decimal)currentWeeklyElectricity - lastWeeksElectricityTotal) / lastWeeksElectricityTotal * 100;
                wastePercentageChange = ((decimal)wasteCurrentWeek - lastWeeksTotal) / lastWeeksTotal * 100;
            }
          

        }
    }
}
