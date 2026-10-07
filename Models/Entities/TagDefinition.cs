namespace MetalCoreHMIOverview.Models.Entities
{
    /// <summary>Loại giá trị của một tag.</summary>
    public enum TagMetric
    {
        Status = 0,     // 1 = START, 0 = STOP
        Pressure = 1,   // MM
        Actual = 2,     // nhiệt độ (°C) hoặc độ nhớt thực tế (s)
        Standard = 3    // giá trị chuẩn đọc từ PLC (VISCOSITY TC)
    }

    /// <summary>Khai báo một tag Kepware (OPC UA node) và ngữ cảnh hiển thị của nó.</summary>
    public class TagDefinition
    {
        public int Id { get; set; }

        /// <summary>Tên dễ đọc, duy nhất. Ví dụ: M01.Stage1.Actual</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>OPC UA NodeId trong Kepware. Ví dụ: ns=2;s=Channel1.Device1.Tag1</summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>Số máy CTL Metalcore (1..8).</summary>
        public int MachineNo { get; set; }

        /// <summary>Nhóm: Machine, Temperature, Stage1, Stage2.</summary>
        public string Section { get; set; } = string.Empty;

        public TagMetric Metric { get; set; }
        public string Unit { get; set; } = string.Empty;

        /// <summary>Giá trị chuẩn và dung sai (chỉ dùng cho Metric = Actual).</summary>
        public double? Standard { get; set; }
        public double? Tolerance { get; set; }

        /// <summary>Hệ số nhân áp dụng cho giá trị thô của PLC (ví dụ 0.1 khi PLC lưu 159 cho 15.9 s).</summary>
        public double Scale { get; set; } = 1;

        public bool IsEnabled { get; set; } = true;

        public List<TagReading> Readings { get; set; } = new();
    }
}
