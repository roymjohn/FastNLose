using SQLite;
using FastNLose.Models;

namespace FastNLose.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseService(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<TrackingItem>().Wait();
            _database.CreateTableAsync<Settings>().Wait(); // create settings table
        }

        // TrackingItem methods
        public Task<int> SaveTrackingItemAsync(TrackingItem item)
        {
            return _database.InsertAsync(item);
        }

        public Task<int> UpdateTrackingItemAsync(TrackingItem item)
        {
            return _database.UpdateAsync(item);
        }

        public Task<List<TrackingItem>> GetTrackingItemsAsync()
        {
            return _database.Table<TrackingItem>().ToListAsync();
        }

        public async Task<TrackingItem> GetActiveTrackingItemAsync()
        {
            return await _database.Table<TrackingItem>()
                .Where(t => t.EndTime == null)
                .OrderByDescending(t => t.StartTime)
                .FirstOrDefaultAsync();
        }

        // Settings methods
        public async Task<Settings> GetSettingsAsync()
        {
            var settings = await _database.Table<Settings>().FirstOrDefaultAsync();
            return settings;
        }

        public async Task SaveSettingsAsync(Settings settings)
        {
            var existing = await _database.Table<Settings>().FirstOrDefaultAsync();
            if (existing != null)
            {
                settings.Id = existing.Id;
                await _database.UpdateAsync(settings);
            }
            else
            {
                await _database.InsertAsync(settings);
            }
        }

        public Task<int> ClearAllAsync()
        {
            return _database.DeleteAllAsync<TrackingItem>();
        }

        public async Task DeleteShortSessionsBeforeLastAsync(int lastId)
        {
            var items = await _database.Table<TrackingItem>()
                                       .OrderBy(i => i.StartTime)
                                       .ToListAsync();

            foreach (var item in items)
            {
                if (item.Id == lastId)
                    continue; // never delete the last record

                if (item.EndTime.HasValue)
                {
                    var duration = item.EndTime.Value - item.StartTime;

                    if (duration.TotalHours < 5)
                        await _database.DeleteAsync(item);
                }
            }
        }

    }
}
