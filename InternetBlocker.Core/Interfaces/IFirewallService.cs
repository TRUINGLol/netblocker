using InternetBlocker.Core.Models;

namespace InternetBlocker.Core.Interfaces;

public interface IFirewallService
{
    bool IsElevated { get; }
    Task<bool> BlockEntityAsync(BlockedEntity entity);
    Task<bool> UnblockEntityAsync(string entityId);
    Task<IEnumerable<BlockedEntity>> GetBlockedEntitiesAsync();
    Task TrackConnectionsAsync(IEnumerable<ConnectionInfo> connections);
}
