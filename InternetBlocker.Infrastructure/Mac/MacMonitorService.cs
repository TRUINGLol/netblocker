using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;

namespace InternetBlocker.Infrastructure.Mac;

public class MacMonitorService : IMonitorService
{
    private readonly Dictionary<int, string> _pathCache = new();

    public IEnumerable<ConnectionInfo> GetActiveConnections() => GetActiveConnectionsAsync().GetAwaiter().GetResult();

    public async Task<IEnumerable<ConnectionInfo>> GetActiveConnectionsAsync()
    {
        var connections = new List<ConnectionInfo>();
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "lsof",
                Arguments = "-i -n -P",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return connections;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var seenThisIteration = new HashSet<int>();

            foreach (var line in lines.Skip(1))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 9) continue;

                var processName = parts[0];
                if (!int.TryParse(parts[1], out int pid)) continue;
                var protocol = parts[7].Equals("UDP", StringComparison.OrdinalIgnoreCase) ? NetworkProtocol.Udp : NetworkProtocol.Tcp;
                
                var addressInfo = parts[8].Split("->");
                string localStr = addressInfo[0];
                string remoteStr = addressInfo.Length > 1 ? addressInfo[1] : "";

                var localParts = localStr.Split(':');
                var remoteParts = remoteStr.Split(':');

                var localAddr = localParts[0];
                var localPort = localParts.Length > 1 && int.TryParse(localParts[1], out var lp) ? lp : 0;
                
                var remoteAddr = remoteParts.Length > 0 ? remoteParts[0] : "";
                var remotePort = remoteParts.Length > 1 && int.TryParse(remoteParts[1], out var rp) ? rp : 0;

                var status = remoteStr.Contains("ESTABLISHED") || parts.Last().Contains("(ESTABLISHED)") 
                    ? ConnectionStatus.Active 
                    : (parts.Last().Contains("(LISTEN)") ? ConnectionStatus.Listen : ConnectionStatus.Closed);

                if (!_pathCache.TryGetValue(pid, out var processPath))
                {
                    processPath = GetProcessPath(pid);
                    _pathCache[pid] = processPath;
                }
                seenThisIteration.Add(pid);

                connections.Add(new ConnectionInfo(
                    pid,
                    processName,
                    processPath,
                    localAddr,
                    localPort,
                    remoteAddr,
                    remotePort,
                    protocol,
                    status
                ));
            }

            // Cleanup cache
            var toRemove = _pathCache.Keys.Where(k => !seenThisIteration.Contains(k)).ToList();
            foreach (var k in toRemove) _pathCache.Remove(k);
        }
        catch { }

        return connections;
    }

    private string GetProcessPath(int pid)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = $"-p {pid} -o comm=",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            return process?.StandardOutput.ReadToEnd().Trim() ?? "Unknown";
        }
        catch { return "Unknown"; }
    }
}
