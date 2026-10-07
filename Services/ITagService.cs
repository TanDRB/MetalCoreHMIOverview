using MetalCoreHMIOverview.Models.Dtos;

namespace MetalCoreHMIOverview.Services
{
    /// <summary>Dữ liệu tag cho giao diện: giá trị mới nhất, lịch sử, trạng thái kết nối.</summary>
    public interface ITagService
    {
        IReadOnlyList<TagValueDto> GetLatest(int? machineNo = null, string? section = null);
        Task<IReadOnlyList<TagReadingDto>> GetHistoryAsync(int tagId, DateTime? fromUtc, DateTime? toUtc, int take, CancellationToken ct = default);
        OpcStatusDto GetStatus();
        Task<IReadOnlyList<OpcNodeDto>> BrowseAsync(string? nodeId, CancellationToken ct = default);
    }
}
