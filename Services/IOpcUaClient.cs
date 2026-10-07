using MetalCoreHMIOverview.Models.Dtos;
using MetalCoreHMIOverview.Models.Entities;

namespace MetalCoreHMIOverview.Services
{
    public interface IOpcUaClient : IAsyncDisposable
    {
        bool IsConnected { get; }
        string? LastError { get; }

        Task<IReadOnlyList<OpcReadResult>> ReadAsync(IReadOnlyList<TagDefinition> tags, CancellationToken ct);

        /// <summary>Duyệt cây node của Kepware (để tìm NodeId khi cấu hình tag). nodeId = null: bắt đầu từ ObjectsFolder.</summary>
        Task<IReadOnlyList<OpcNodeDto>> BrowseAsync(string? nodeId, CancellationToken ct);
    }
}
