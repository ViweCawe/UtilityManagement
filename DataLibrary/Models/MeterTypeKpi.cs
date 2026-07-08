using System;
using System.Collections.Generic;
using System.Text;

namespace DataLibrary.Models
{
    public class MeterTypeKpi
    {
        public MeterType MeterType { get; set; }

        public int Current7Days { get; set; }
        public int Previous7Days { get; set; }
        public double WeeklyGrowthPercent { get; set; }

        public int Current30Days { get; set; }
        public int Previous30Days { get; set; }
        public double MonthlyGrowthPercent { get; set; }
    }
}
