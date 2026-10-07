using MetalCoreHMIOverview.Models.Entities;

namespace MetalCoreHMIOverview.Repositories
{
    public interface ITagDefinitionRepository
    {
        Task<List<TagDefinition>> GetAllAsync(CancellationToken ct = default);
        Task<List<TagDefinition>> GetEnabledAsync(CancellationToken ct = default);
        Task<TagDefinition?> GetByIdAsync(int id, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<TagDefinition> tags, CancellationToken ct = default);
        Task UpdateAsync(TagDefinition tag, CancellationToken ct = default);
    }
}
