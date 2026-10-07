namespace MetalCoreHMIOverview.Models
{
    /// <summary>Trạng thái một máy CTL Metalcore hiển thị trên HMI.</summary>
    public class MachineStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>START / STOP</summary>
        public string Status { get; set; } = "STOP";

        public double StandardTemp { get; set; }
        public double Tolerance { get; set; }
        public double ActualTemp { get; set; }

        public double MinTemp => StandardTemp - Tolerance;
        public double MaxTemp => StandardTemp + Tolerance;

        /// <summary>OK khi nhiệt độ thực tế nằm trong khoảng tiêu chuẩn ± dung sai.</summary>
        public string Result => ActualTemp >= MinTemp && ActualTemp <= MaxTemp ? "OK" : "NG";
    }

    public class HmiOverviewViewModel
    {
        public DateTime Timestamp { get; set; }
        public int RefreshIntervalMs { get; set; }
        public List<MachineStatus> Machines { get; set; } = new();
    }

    /// <summary>Cấu hình đọc từ appsettings.json, mục "Hmi".</summary>
    public class HmiOptions
    {
        public int RefreshIntervalMs { get; set; } = 2000;
        public double StandardTemp { get; set; } = 75;
        public double Tolerance { get; set; } = 10;
        public List<string> Machines { get; set; } = new();
    }
}
