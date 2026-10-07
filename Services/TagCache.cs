using System.Collections.Concurrent;
using MetalCoreHMIOverview.Models.Dtos;
using MetalCoreHMIOverview.Models.Entities;

namespace MetalCoreHMIOverview.Services
{
    /// <summary>Bộ nhớ đệm giá trị mới nhất của từng tag (giao diện đọc từ đây, không chờ SQL).</summary>
    public interface ITagCache
    {
        void Update(IEnumerable<TagDefinition> tags, IEnumerable<OpcReadResult> results);
        IReadOnlyList<TagValueDto> GetAll();
        DateTime? LastReadUtc { get; }
        int TagCount { get; }

        /// <summary>Tăng mỗi khi có giá trị hoặc chất lượng của bất kỳ tag nào thay đổi.</summary>
        long Version { get; }

        /// <summary>Chờ đến khi Version khác lastVersion (hoặc hết timeout) rồi trả về Version hiện tại.</summary>
        Task<long> WaitForChangeAsync(long lastVersion, TimeSpan timeout, CancellationToken ct);
    }

    public sealed class TagCache : ITagCache
    {
        private readonly ConcurrentDictionary<int, TagValueDto> _values = new();
        private readonly object _sync = new();
        private TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private long _lastTicks;
        private long _version;

        public DateTime? LastReadUtc => _lastTicks == 0 ? null : new DateTime(Interlocked.Read(ref _lastTicks), DateTimeKind.Utc);
        public int TagCount => _values.Count;
        public long Version => Interlocked.Read(ref _version);

        public void Update(IEnumerable<TagDefinition> tags, IEnumerable<OpcReadResult> results)
        {
            var map = tags.ToDictionary(t => t.Id);
            var changed = false;

            foreach (var raw in results)
            {
                if (!map.TryGetValue(raw.TagId, out var t)) continue;

                // Kepware trả Bad kèm giá trị rỗng: giữ lại giá trị đã biết gần nhất để vẫn hiển thị
                var r = raw;
                if (r.Value == null && _values.TryGetValue(r.TagId, out var prev) && prev.Value != null)
                    r = new OpcReadResult(r.TagId, prev.Value, false, prev.TimestampUtc);

                if (!_values.TryGetValue(r.TagId, out var old) || old.Value != r.Value || old.IsGood != r.IsGood
                    || old.Standard != t.Standard || old.Tolerance != t.Tolerance)
                    changed = true;

                _values[r.TagId] = new TagValueDto
                {
                    TagId = t.Id,
                    Name = t.Name,
                    MachineNo = t.MachineNo,
                    Section = t.Section,
                    Metric = t.Metric,
                    Unit = t.Unit,
                    Standard = t.Standard,
                    Tolerance = t.Tolerance,
                    Value = r.Value,
                    IsGood = r.IsGood,
                    TimestampUtc = r.TimestampUtc
                };
                Interlocked.Exchange(ref _lastTicks, Math.Max(Interlocked.Read(ref _lastTicks), raw.TimestampUtc.Ticks));
            }

            // Bỏ tag đã bị tắt / xóa khỏi database
            foreach (var id in _values.Keys.Where(id => !map.ContainsKey(id)))
                if (_values.TryRemove(id, out _)) changed = true;

            if (changed) Signal();
        }

        public IReadOnlyList<TagValueDto> GetAll() =>
            _values.Values.OrderBy(v => v.MachineNo).ThenBy(v => v.Name).ToList();

        public async Task<long> WaitForChangeAsync(long lastVersion, TimeSpan timeout, CancellationToken ct)
        {
            Task waiter;
            lock (_sync)
            {
                if (Version != lastVersion) return Version;
                waiter = _changed.Task;
            }

            await Task.WhenAny(waiter, Task.Delay(timeout, ct));
            ct.ThrowIfCancellationRequested();
            return Version;
        }

        private void Signal()
        {
            TaskCompletionSource old;
            lock (_sync)
            {
                Interlocked.Increment(ref _version);
                old = _changed;
                _changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            old.TrySetResult();
        }
    }
}
