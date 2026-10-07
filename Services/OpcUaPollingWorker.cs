using MetalCoreHMIOverview.Models.Dtos;
using MetalCoreHMIOverview.Models.Entities;
using MetalCoreHMIOverview.Models.Options;
using MetalCoreHMIOverview.Repositories;
using Microsoft.Extensions.Options;

namespace MetalCoreHMIOverview.Services
{
    /// <summary>
    /// Chạy nền: đọc tag từ Kepware theo chu kỳ, cập nhật cache và ghi lịch sử vào SQL Server.
    /// </summary>
    public class OpcUaPollingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IOpcUaClient _opc;
        private readonly ITagCache _cache;
        private readonly OpcUaOptions _opt;
        private readonly ILogger<OpcUaPollingWorker> _log;

        public OpcUaPollingWorker(IServiceScopeFactory scopes, IOpcUaClient opc, ITagCache cache,
            IOptions<OpcUaOptions> options, ILogger<OpcUaPollingWorker> log)
        {
            _scopes = scopes;
            _opc = opc;
            _cache = cache;
            _opt = options.Value;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tags = new List<TagDefinition>();
            var nextReload = DateTime.MinValue;
            var nextPersist = DateTime.UtcNow.AddSeconds(_opt.PersistIntervalSec);
            var nextCleanup = DateTime.UtcNow.AddMinutes(1);
            var pending = new List<TagReading>();
            var seeded = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1) Nạp danh sách tag từ database định kỳ
                    if (DateTime.UtcNow >= nextReload)
                    {
                        using var scope = _scopes.CreateScope();
                        tags = await scope.ServiceProvider.GetRequiredService<ITagDefinitionRepository>().GetEnabledAsync(stoppingToken);

                        if (!seeded)
                        {
                            seeded = true;
                            await SeedLastKnownAsync(scope, tags, stoppingToken);
                        }
                        nextReload = DateTime.UtcNow.AddSeconds(_opt.ReloadTagsIntervalSec);
                    }

                    // 2) Đọc từ Kepware và cập nhật cache
                    var raw = await _opc.ReadAsync(tags, stoppingToken);
                    var scale = tags.ToDictionary(t => t.Id, t => t.Scale);
                    var results = raw
                        .Select(r => r.Value.HasValue && scale.TryGetValue(r.TagId, out var k) && k != 1
                            ? r with { Value = Math.Round(r.Value.Value * k, 4) }
                            : r)
                        .ToList();
                    _cache.Update(tags, results);

                    // 3) Gom dữ liệu, ghi lịch sử mỗi PersistIntervalSec giây (lưu mọi giá trị đọc được, kể cả chất lượng Bad)
                    pending.AddRange(results.Where(r => r.Value.HasValue).Select(r => new TagReading
                    {
                        TagDefinitionId = r.TagId,
                        Value = r.Value,
                        IsGood = r.IsGood,
                        TimestampUtc = r.TimestampUtc
                    }));

                    if (DateTime.UtcNow >= nextPersist)
                    {
                        nextPersist = DateTime.UtcNow.AddSeconds(_opt.PersistIntervalSec);
                        await PersistAsync(pending, stoppingToken);
                    }

                    // 4) Dọn lịch sử cũ
                    if (_opt.RetentionDays > 0 && DateTime.UtcNow >= nextCleanup)
                    {
                        nextCleanup = DateTime.UtcNow.AddHours(1);
                        using var scope = _scopes.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<ITagReadingRepository>();
                        var n = await repo.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-_opt.RetentionDays), stoppingToken);
                        if (n > 0) _log.LogInformation("Đã xóa {Count} bản ghi lịch sử cũ", n);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Lỗi vòng đọc dữ liệu");
                }

                try { await Task.Delay(_opt.PollIntervalMs, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        /// <summary>Nạp giá trị cuối cùng từ SQL Server để có số hiển thị ngay cả khi Kepware chưa trả dữ liệu (tag Bad).</summary>
        private async Task SeedLastKnownAsync(IServiceScope scope, List<TagDefinition> tags, CancellationToken ct)
        {
            try
            {
                var last = await scope.ServiceProvider.GetRequiredService<ITagReadingRepository>().GetLatestPerTagAsync(ct);
                _cache.Update(tags, last.Select(r => new OpcReadResult(r.TagDefinitionId, r.Value, false, r.TimestampUtc)));
                _log.LogInformation("Đã nạp {Count} giá trị cuối cùng từ database", last.Count);
            }
            catch (Exception ex)
            {
                _log.LogWarning("Không nạp được giá trị cuối từ database: {Message}", ex.Message);
            }
        }

        private async Task PersistAsync(List<TagReading> pending, CancellationToken ct)
        {
            if (pending.Count == 0) return;
            try
            {
                using var scope = _scopes.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ITagReadingRepository>();
                await repo.AddRangeAsync(pending, ct);
                pending.Clear();
            }
            catch (Exception ex)
            {
                // Giữ lại để thử ghi lần sau, nhưng không để bộ nhớ phình ra khi SQL Server tắt
                _log.LogWarning("Ghi SQL Server lỗi: {Message}", ex.Message);
                if (pending.Count > 50_000) pending.RemoveRange(0, pending.Count - 50_000);
            }
        }
    }
}
