using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastNLose.Models
{
    public class DayRecord
    {
        public DateTime Date { get; set; }
        public Dictionary<string, DailySectionState> Sections { get; set; } = new();
        public int? NumericValue { get; set; }
        public bool? YesNoValue { get; set; }
    }

}
