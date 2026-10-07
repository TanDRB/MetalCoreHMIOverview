using MetalCoreHMIOverview.Models.Entities;

namespace MetalCoreHMIOverview.Models.Dtos
{
    public class TagValueDto
    {
        public int TagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MachineNo { get; set; }
        public string Section { get; set; } = string.Empty;
        public TagMetric Metric { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double? Value { get; set; }
        public double? Standard { get; set; }
        public double? Tolerance { get; set; }
        public bool IsGood { get; set; }
        public DateTime TimestampUtc { get; set; }

        /// <summary>OK / NG (chỉ có với Metric = Actual và có đủ chuẩn, dung sai).</summary>
        public string? Result =>
            Metric == TagMetric.Actual && Value.HasValue && Standard.HasValue && Tolerance.HasValue
                ? (Math.Abs(Value.Value - Standard.Value) <= Tolerance.Value + 1e-9 ? "OK" : "NG")
                : null;
    }

    public class TagReadingDto
    {
        public DateTime TimestampUtc { get; set; }
        public double? Value { get; set; }
        public bool IsGood { get; set; }
    }

    public record OpcReadResult(int TagId, double? Value, bool IsGood, DateTime TimestampUtc);

    public record OpcNodeDto(string NodeId, string DisplayName, string NodeClass, bool HasChildren);

    public class OpcStatusDto
    {
        public bool IsConnected { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public DateTime? LastReadUtc { get; set; }
        public string? LastError { get; set; }
        public int TagCount { get; set; }
    }
}
