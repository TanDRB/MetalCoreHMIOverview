using MetalCoreHMIOverview.Data;
using MetalCoreHMIOverview.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalCoreHMIOverview.Repositories
{
    public class TagDefinitionRepository : ITagDefinitionRepository
    {
        private readonly AppDbContext _db;
        public TagDefinitionRepository(AppDbContext db) => _db = db;

        public Task<List<TagDefinition>> GetAllAsync(CancellationToken ct = default) =>
            _db.TagDefinitions.AsNoTracking().OrderBy(t => t.MachineNo).ThenBy(t => t.Name).ToListAsync(ct);

        public Task<List<TagDefinition>> GetEnabledAsync(CancellationToken ct = default) =>
            _db.TagDefinitions.AsNoTracking().Where(t => t.IsEnabled).OrderBy(t => t.Id).ToListAsync(ct);

        public Task<TagDefinition?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _db.TagDefinitions.FirstOrDefaultAsync(t => t.Id == id, ct);

        public async Task AddRangeAsync(IEnumerable<TagDefinition> tags, CancellationToken ct = default)
        {
            _db.TagDefinitions.AddRange(tags);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(TagDefinition tag, CancellationToken ct = default)
        {
            _db.TagDefinitions.Update(tag);
            await _db.SaveChangesAsync(ct);
        }
    }
}
