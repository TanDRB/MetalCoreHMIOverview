using MetalCoreHMIOverview.Models.Dtos;
using MetalCoreHMIOverview.Models.Entities;
using MetalCoreHMIOverview.Models.Options;
using MetalCoreHMIOverview.Repositories;
using Microsoft.Extensions.Options;

namespace MetalCoreHMIOverview.Services
{
    public class TagService : ITagService
    {
        private readonly ITagCache _cache;
        private readonly IOpcUaClient _opc;
        private readonly ITagReadingRepository _readings;
        private readonly OpcUaOptions _opt;

        public TagService(ITagCache cache, IOpcUaClient opc, ITagReadingRepository readings, IOptions<OpcUaOptions> options)
        {
            _cache = cache;
            _opc = opc;
            _readings = readings;
            _opt = options.Value;
        }

        public IReadOnlyList<TagValueDto> GetLatest(int? machineNo = null, string? section = null)
        {
            var all = _cache.GetAll();

            // Giá trị chuẩn đọc được từ PLC (VISCOSITY TC) thay cho giá trị chuẩn cấu hình sẵn
            var plcStandard = all
                .Where(v => v.Metric == TagMetric.Standard && v.Value.HasValue)
                .ToDictionary(v => (v.MachineNo, v.Section), v => v.Value);
            foreach (var v in all.Where(v => v.Metric == TagMetric.Actual))
                if (plcStandard.TryGetValue((v.MachineNo, v.Section), out var std))
                    v.Standard = std;

            IEnumerable<TagValueDto> q = all;
            if (machineNo.HasValue) q = q.Where(v => v.MachineNo == machineNo.Value);
            if (!string.IsNullOrWhiteSpace(section)) q = q.Where(v => v.Section.Equals(section, StringComparison.OrdinalIgnoreCase));
            return q.ToList();
        }

        public async Task<IReadOnlyList<TagReadingDto>> GetHistoryAsync(int tagId, DateTime? fromUtc, DateTime? toUtc, int take, CancellationToken ct = default)
        {
            var to = toUtc ?? DateTime.UtcNow;
            var from = fromUtc ?? to.AddHours(-1);
            take = Math.Clamp(take, 1, 5000);

            var rows = await _readings.GetHistoryAsync(tagId, from, to, take, ct);
            return rows.Select(r => new TagReadingDto { TimestampUtc = r.TimestampUtc, Value = r.Value, IsGood = r.IsGood }).ToList();
        }

        public Task<IReadOnlyList<OpcNodeDto>> BrowseAsync(string? nodeId, CancellationToken ct = default) =>
            _opc.BrowseAsync(nodeId, ct);

        public OpcStatusDto GetStatus() => new()
        {
            IsConnected = _opc.IsConnected,
            Endpoint = _opt.EndpointUrl,
            LastReadUtc = _cache.LastReadUtc,
            LastError = _opc.LastError,
            TagCount = _cache.TagCount
        };
    }
}
