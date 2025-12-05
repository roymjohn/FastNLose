using SQLite;
using System;


namespace FastNLose.Models
{
    public class TimeRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime StopTime { get; set; }
        public double ElapsedHours { get; set; }
        public DateTime Date { get; set; }
    }
}