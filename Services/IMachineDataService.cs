using MetalCoreHMIOverview.Models;

namespace MetalCoreHMIOverview.Services
{
    /// <summary>
    /// Nguồn dữ liệu máy. Hiện dùng bản mô phỏng (SimulatedMachineDataService);
    /// khi kết nối PLC thật (OPC UA, Modbus, MC Protocol...) chỉ cần viết một class
    /// khác implement interface này và đổi đăng ký trong Program.cs.
    /// </summary>
    public interface IMachineDataService
    {
        IReadOnlyList<MachineStatus> GetMachines();
    }
}
