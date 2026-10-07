namespace MetalCoreHMIOverview.Models.Options
{
    /// <summary>Cấu hình kết nối Kepware (OPC UA), mục "OpcUa" trong appsettings.json.</summary>
    public class OpcUaOptions
    {
        public string EndpointUrl { get; set; } = "opc.tcp://192.168.41.30:49320";
        public string ApplicationName { get; set; } = "MetalCoreHMI";

        /// <summary>Để trống nếu Kepware cho phép Anonymous.</summary>
        public string? Username { get; set; }
        public string? Password { get; set; }

        public int SessionTimeoutMs { get; set; } = 60000;
        public int PollIntervalMs { get; set; } = 1000;

        /// <summary>Chu kỳ ghi lịch sử vào SQL Server (giây).</summary>
        public int PersistIntervalSec { get; set; } = 10;

        /// <summary>Chu kỳ nạp lại danh sách tag từ database (giây).</summary>
        public int ReloadTagsIntervalSec { get; set; } = 60;

        /// <summary>Số ngày giữ lịch sử, 0 = không xóa.</summary>
        public int RetentionDays { get; set; } = 30;
    }
}
