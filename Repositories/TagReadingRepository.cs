using MetalCoreHMIOverview.Data;
using MetalCoreHMIOverview.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalCoreHMIOverview.Repositories
{
    public class TagReadingRepository : ITagReadingRepository
    {
        private readonly AppDbContext _db;
        public TagReadingRepository(AppDbContext db) => _db = db;

        public async Task AddRangeAsync(IEnumerable<TagReading> readings, CancellationToken ct = default)
        {
            _db.TagReadings.AddRange(readings);
            await _db.SaveChangesAsync(ct);
            _db.ChangeTracker.Clear();
        }

        public Task<List<TagReading>> GetHistoryAsync(int tagId, DateTime fromUtc, DateTime toUtc, int take, CancellationToken ct = default) =>
            _db.TagReadings.AsNoTracking()
                .Where(r => r.TagDefinitionId == tagId && r.TimestampUtc >= fromUtc && r.TimestampUtc <= toUtc)
                .OrderByDescending(r => r.TimestampUtc)
                .Take(take)
                .ToListAsync(ct);

        public Task<List<TagReading>> GetLatestPerTagAsync(CancellationToken ct = default) =>
            _db.TagReadings.AsNoTracking()
                .GroupBy(r => r.TagDefinitionId)
                .Select(g => g.OrderByDescending(x => x.TimestampUtc).First())
                .ToListAsync(ct);

        public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default) =>
            _db.TagReadings.Where(r => r.TimestampUtc < cutoffUtc).ExecuteDeleteAsync(ct);
    }
}
