using MetalCoreHMIOverview.Models.Entities;

namespace MetalCoreHMIOverview.Repositories
{
    public interface ITagReadingRepository
    {
        Task AddRangeAsync(IEnumerable<TagReading> readings, CancellationToken ct = default);
        Task<List<TagReading>> GetHistoryAsync(int tagId, DateTime fromUtc, DateTime toUtc, int take, CancellationToken ct = default);
        Task<List<TagReading>> GetLatestPerTagAsync(CancellationToken ct = default);
        Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default);
    }
}
