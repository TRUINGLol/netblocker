namespace InternetBlocker.Core.Models;

public enum NetworkProtocol
{
    Tcp,
    Udp
}

public enum ConnectionStatus
{
    Active,
    Listen,
    Closed,
    Blocked
}

public record ConnectionInfo(
    int ProcessId,
    string ProcessName,
    string ProcessPath,
    string LocalAddress,
    int LocalPort,
    string RemoteAddress,
    int RemotePort,
    NetworkProtocol Protocol,
    ConnectionStatus Status,
    byte[]? IconData = null
);
