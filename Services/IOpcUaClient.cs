using MetalCoreHMIOverview.Models.Dtos;
using MetalCoreHMIOverview.Models.Entities;

namespace MetalCoreHMIOverview.Services
{
    /// <summary>Kết nối OPC UA tới Kepware và đọc giá trị tag.</summary>
    public interface IOpcUaClient : IAsyncDisposable
    {
        bool IsConnected { get; }
        string? LastError { get; }

        /// <summary>Đọc một lượt tất cả tag. Tự kết nối lại nếu mất kết nối.</summary>
        Task<IReadOnlyList<OpcReadResult>> ReadAsync(IReadOnlyList<TagDefinition> tags, CancellationToken ct);

        /// <summary>Duyệt cây node của Kepware (để tìm NodeId khi cấu hình tag). nodeId = null: bắt đầu từ ObjectsFolder.</summary>
        Task<IReadOnlyList<OpcNodeDto>> BrowseAsync(string? nodeId, CancellationToken ct);
    }
}
