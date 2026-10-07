using MetalCoreHMIOverview.Models.Entities;

namespace MetalCoreHMIOverview.Repositories
{
    public interface ITagReadingRepository
    {
        Task AddRangeAsync(IEnumerable<TagReading> readings, CancellationToken ct = default);
        Task<List<TagReading>> GetHistoryAsync(int tagId, DateTime fromUtc, DateTime toUtc, int take, CancellationToken ct = default);
        /// <summary>Bản ghi mới nhất của từng tag (dùng để nạp lại giá trị cuối cùng khi khởi động).</summary>
        Task<List<TagReading>> GetLatestPerTagAsync(CancellationToken ct = default);
        Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default);
    }
}
