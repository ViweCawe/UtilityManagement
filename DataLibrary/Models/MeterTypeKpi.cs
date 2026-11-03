using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Models
{
    public class MeterTypeKpi
    {
        public bool WeeklyAvailable { get; set; }
        public bool MonthlyAvailable { get; set; }
        public MeterType MeterType { get; set; }

        public decimal Current7Days { get; set; }
        public decimal Previous7Days { get; set; }
        public double WeeklyGrowthPercent { get; set; }

        public decimal Current30Days { get; set; }
        public decimal Previous30Days { get; set; }
        public double MonthlyGrowthPercent { get; set; }
    }
}
