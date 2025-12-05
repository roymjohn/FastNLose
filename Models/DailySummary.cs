namespace FastNLose.Models
{
    public class DailySummary
    {
        public string Date { get; set; }   // mm/dd
        public int H2 { get; set; }
        public int Wk { get; set; }
        public string Fs { get; set; }     // hh:mm form
    }
}
