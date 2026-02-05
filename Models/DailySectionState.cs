using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastNLose.Models
{
    public class DailySectionState
    {
        public string SectionKey { get; set; }
        public int[] States { get; set; } // 0 yellow, 1 red, 2 green
    }

}
