using SQLite;
using System;


namespace FastNLose.Models
{
    public class Settings
    {
        [PrimaryKey]
        public int Id { get; set; } = 1;
        public double TargetTotalHours { get; set; }
        public TimeSpan ExpectedStart { get; set; }
        public TimeSpan ExpectedStop { get; set; }
        public int Score { get; set; }
    }
}