using InternetBlocker.Core.Models;

namespace InternetBlocker.Core.Interfaces;

public interface IBlockedEntityRepository
{
    Task<IEnumerable<BlockedEntity>> GetAllAsync();
    Task AddAsync(BlockedEntity entity);
    Task DeleteAsync(string id);
    Task UpdateAsync(BlockedEntity entity);
}
