using MetalCoreHMIOverview.Models;
using Microsoft.Extensions.Options;

namespace MetalCoreHMIOverview.Services
{
    /// <summary>
    /// Dữ liệu mô phỏng: nhiệt độ dao động nhẹ quanh giá trị ban đầu để kiểm tra giao diện.
    /// </summary>
    public class SimulatedMachineDataService : IMachineDataService
    {
        private readonly object _lock = new();
        private readonly Random _random = new();
        private readonly List<MachineStatus> _machines;

        public SimulatedMachineDataService(IOptions<HmiOptions> options)
        {
            var opt = options.Value;
            var names = opt.Machines.Count > 0
                ? opt.Machines
                : Enumerable.Range(1, 5).Select(i => $"CTL METALCORE #{i}").ToList();

            double[] initial = { 82.0, 76.0, 75.6, 76.0, 77.0 };

            _machines = names.Select((name, i) => new MachineStatus
            {
                Id = i + 1,
                Name = name,
                Status = "START",
                StandardTemp = opt.StandardTemp,
                Tolerance = opt.Tolerance,
                ActualTemp = i < initial.Length ? initial[i] : opt.StandardTemp
            }).ToList();
        }

        public IReadOnlyList<MachineStatus> GetMachines()
        {
            lock (_lock)
            {
                foreach (var m in _machines)
                {
                    // Dao động ±0.3 °C, kéo nhẹ về giá trị tiêu chuẩn
                    var drift = (_random.NextDouble() - 0.5) * 0.6;
                    var pull = (m.StandardTemp - m.ActualTemp) * 0.02;
                    m.ActualTemp = Math.Round(m.ActualTemp + drift + pull, 1);
                }

                // Trả về bản sao để tránh bị sửa ngoài lock
                return _machines.Select(m => new MachineStatus
                {
                    Id = m.Id,
                    Name = m.Name,
                    Status = m.Status,
                    StandardTemp = m.StandardTemp,
                    Tolerance = m.Tolerance,
                    ActualTemp = m.ActualTemp
                }).ToList();
            }
        }
    }
}
