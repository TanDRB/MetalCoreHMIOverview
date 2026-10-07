using MetalCoreHMIOverview.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalCoreHMIOverview.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TagDefinition> TagDefinitions => Set<TagDefinition>();
        public DbSet<TagReading> TagReadings => Set<TagReading>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<TagDefinition>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.Property(x => x.NodeId).HasMaxLength(300).IsRequired();
                e.Property(x => x.Section).HasMaxLength(50).IsRequired();
                e.Property(x => x.Unit).HasMaxLength(20);
                e.Property(x => x.Metric).HasConversion<int>();
                e.Property(x => x.Scale).HasDefaultValue(1.0);
                e.HasIndex(x => x.Name).IsUnique();
                e.HasIndex(x => new { x.MachineNo, x.Section, x.Metric });
            });

            b.Entity<TagReading>(e =>
            {
                e.HasOne(x => x.TagDefinition)
                    .WithMany(t => t.Readings)
                    .HasForeignKey(x => x.TagDefinitionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.TagDefinitionId, x.TimestampUtc });
            });
        }
    }
}
