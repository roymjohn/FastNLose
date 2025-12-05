using System;
using SQLite;

namespace FastNLose.Models
{
    public class TrackingItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // persisted fields
        public string Date { get; set; }               // yyyy-MM-dd
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // convenience (not persisted) — format values for UI
        [Ignore]
        public string StartTimeText => StartTime.ToString("t"); // e.g. 9:30 AM

        [Ignore]
        public string EndTimeText => EndTime?.ToString("t") ?? "--:--";

        // elapsed, if end time exists
        [Ignore]
        public TimeSpan? Elapsed => EndTime.HasValue ? (EndTime - StartTime) : (TimeSpan?)null;

        // This is what the CollectionView will bind to
        [Ignore]
        public string DisplayText
        {
            get
            {
                if (EndTime.HasValue)
                {
                    var elapsed = Elapsed ?? TimeSpan.Zero;
                    return $"{Date}  {StartTimeText} – {EndTimeText}  ({elapsed:hh\\:mm\\:ss})";
                }
                else
                {
                    return $"{Date}  {StartTimeText} – --:--  (running)";
                }
            }
        }
    }
}
