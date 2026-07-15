using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataLibrary.Models
{
    public class DailyPeopleCount
    {
        [Key]
        public int Id { get; set; }
        public int Visitors { get; set; }
        public int Employees { get; set; }
        public int TotalPeople => Visitors + Employees;
        public DateTime Date { get; set; } 
    }
}
