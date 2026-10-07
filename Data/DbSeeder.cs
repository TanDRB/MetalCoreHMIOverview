using MetalCoreHMIOverview.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalCoreHMIOverview.Data
{
    /// <summary>
    /// Tạo tag ban đầu cho 8 máy khi bảng TagDefinitions còn trống. NodeId theo channel "Metalcore" trong Kepware:
    /// CTL.on#N (trạng thái), CTL.Temp#N (nhiệt độ), viscosity#N-S.PPM / .VISCOSITY TC / .VISCOSITY TT (máy N, công đoạn S).
    /// </summary>
    public static class DbSeeder
    {
        public const int MachineCount = 8;
        private const string Ch = "ns=2;s=Metalcore";

        public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
        {
            if (await db.TagDefinitions.AnyAsync(ct)) return;

            var list = new List<TagDefinition>();
            for (var m = 1; m <= MachineCount; m++)
            {
                var p = $"M{m:00}";

                list.Add(new TagDefinition { Name = $"{p}.Status", NodeId = $"{Ch}.CTL.on#{m}", MachineNo = m, Section = "Machine", Metric = TagMetric.Status });

                list.Add(new TagDefinition { Name = $"{p}.Temperature.Actual", NodeId = $"{Ch}.CTL.Temp#{m}", MachineNo = m, Section = "Temperature", Metric = TagMetric.Actual, Unit = "°C", Standard = 70, Tolerance = 10 });

                for (var s = 1; s <= 2; s++)
                {
                    var dev = $"{Ch}.viscosity#{m}-{s}";
                    var sec = $"Stage{s}";
                    var std = s == 1 ? 16.0 : 19.0;

                    list.Add(new TagDefinition { Name = $"{p}.{sec}.Pressure", NodeId = $"{dev}.PPM", MachineNo = m, Section = sec, Metric = TagMetric.Pressure, Unit = "MM" });
                    list.Add(new TagDefinition { Name = $"{p}.{sec}.Actual", NodeId = $"{dev}.VISCOSITY TT", MachineNo = m, Section = sec, Metric = TagMetric.Actual, Unit = "s", Standard = std, Tolerance = 0.5, Scale = 0.1 });
                    list.Add(new TagDefinition { Name = $"{p}.{sec}.Standard", NodeId = $"{dev}.VISCOSITY TC", MachineNo = m, Section = sec, Metric = TagMetric.Standard, Unit = "s", Scale = 0.1 });
                }
            }

            db.TagDefinitions.AddRange(list);
            await db.SaveChangesAsync(ct);
        }
    }
}
