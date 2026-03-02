using InternetBlocker.Core.Models;

namespace InternetBlocker.Core.Interfaces;

public interface IMonitorService
{
    IEnumerable<ConnectionInfo> GetActiveConnections();
    Task<IEnumerable<ConnectionInfo>> GetActiveConnectionsAsync();
}
