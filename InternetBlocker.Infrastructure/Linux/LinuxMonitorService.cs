using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;

namespace InternetBlocker.Infrastructure.Linux;

public class LinuxMonitorService : IMonitorService
{
    public IEnumerable<ConnectionInfo> GetActiveConnections() => GetActiveConnectionsAsync().GetAwaiter().GetResult();

    public async Task<IEnumerable<ConnectionInfo>> GetActiveConnectionsAsync()
    {
        var connections = new List<ConnectionInfo>();
        
        try
        {
            connections.AddRange(await ParseProcNetFileAsync("/proc/net/tcp", NetworkProtocol.Tcp));
            connections.AddRange(await ParseProcNetFileAsync("/proc/net/udp", NetworkProtocol.Udp));
        }
        catch { /* Log error */ }
        
        return connections;
    }

    private async Task<List<ConnectionInfo>> ParseProcNetFileAsync(string filePath, NetworkProtocol protocol)
    {
        var result = new List<ConnectionInfo>();
        if (!File.Exists(filePath)) return result;

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            foreach (var line in lines.Skip(1))
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 10) continue;

                var localParts = parts[1].Split(':');
                var remoteParts = parts[2].Split(':');
                if (localParts.Length < 2 || remoteParts.Length < 2) continue;

                var localIp = ParseHexIp(localParts[0]);
                var localPort = int.Parse(localParts[1], NumberStyles.HexNumber);
                var remoteIp = ParseHexIp(remoteParts[0]);
                var remotePort = int.Parse(remoteParts[1], NumberStyles.HexNumber);
                var state = parts[3];
                var inode = parts[9];

                // For the prototype, we leave PID as -1 if we can't easily find it
                // A full implementation would scan /proc/[pid]/fd
                int pid = -1; 
                string processName = "Unknown";
                string processPath = "Unknown";

                result.Add(new ConnectionInfo(
                    pid,
                    processName,
                    processPath,
                    localIp,
                    localPort,
                    remoteIp,
                    remotePort,
                    protocol,
                    MapLinuxStateToStatus(state)
                ));
            }
        }
        catch { }

        return result;
    }

    private string ParseHexIp(string hex)
    {
        if (hex.Length != 8) return hex;
        try
        {
            uint ip = uint.Parse(hex, NumberStyles.HexNumber);
            return $"{(ip & 0xFF)}.{(ip >> 8 & 0xFF)}.{(ip >> 16 & 0xFF)}.{(ip >> 24 & 0xFF)}";
        }
        catch { return "0.0.0.0"; }
    }

    private ConnectionStatus MapLinuxStateToStatus(string stateHex)
    {
        return stateHex switch
        {
            "01" => ConnectionStatus.Active,
            "0A" => ConnectionStatus.Listen,
            _ => ConnectionStatus.Closed
        };
    }
}
