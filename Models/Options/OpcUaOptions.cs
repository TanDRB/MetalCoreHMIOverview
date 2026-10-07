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

        /// <summary>Khoảng cách tối thiểu giữa hai lần đẩy dữ liệu xuống mỗi trình duyệt (ms). 1000 = tối đa 1 lần mỗi giây.</summary>
        public int UiRefreshMs { get; set; } = 1000;

        public int PersistIntervalSec { get; set; } = 10;

        public int ReloadTagsIntervalSec { get; set; } = 60;

        /// <summary>Số ngày giữ lịch sử, 0 = không xóa.</summary>
        public int RetentionDays { get; set; } = 30;
    }
}
