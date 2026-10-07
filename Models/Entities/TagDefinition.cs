namespace MetalCoreHMIOverview.Models.Entities
{
    public enum TagMetric
    {
        Status = 0,     // 1 = START, 0 = STOP
        Pressure = 1,   // MM
        Actual = 2,     // nhiệt độ (°C) hoặc độ nhớt thực tế (s)
        Standard = 3    // giá trị chuẩn đọc từ PLC (VISCOSITY TC)
    }

    public class TagDefinition
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string NodeId { get; set; } = string.Empty;

        public int MachineNo { get; set; }

        public string Section { get; set; } = string.Empty;

        public TagMetric Metric { get; set; }
        public string Unit { get; set; } = string.Empty;

        public double? Standard { get; set; }
        public double? Tolerance { get; set; }

        /// <summary>Hệ số nhân áp dụng cho giá trị thô của PLC (ví dụ 0.1 khi PLC lưu 159 cho 15.9 s).</summary>
        public double Scale { get; set; } = 1;

        public bool IsEnabled { get; set; } = true;

        public List<TagReading> Readings { get; set; } = new();
    }
}
