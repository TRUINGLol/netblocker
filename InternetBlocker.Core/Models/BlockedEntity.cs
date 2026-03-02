namespace InternetBlocker.Core.Models;

public enum BlockedEntityType
{
    Application,
    IpAddress
}

public record BlockedEntity(
    string Id, // Prefixed ID for firewall rule matching
    string Value, // Path for App, IP address for IP
    string Name,
    BlockedEntityType Type,
    bool IsEnabled = true,
    DateTime? CreatedAt = null
);
