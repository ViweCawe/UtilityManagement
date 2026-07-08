using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Models
{
    public class Meter : BaseModel
    {
        public int Id { get; set; }
        public string MeterName { get; set; } = string.Empty;
        public MeterType MeterType { get; set; }
        public string Unit => MeterType switch
        {
            MeterType.Electricity => "kWh",
            MeterType.Water => "L³",
      
        };

        public int AreaId { get; set; }
        public int DepartmentId { get; set; }
        public int StationId { get; set; }


        public string AreaName { get; set; } = string.Empty;
        public string DepartmentName {  get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty ;
        public Boolean IsCumulative { get; set; }
        public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
    }
}

