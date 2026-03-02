using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;

namespace InternetBlocker.Infrastructure.Linux;

public class LinuxFirewallService : IFirewallService
{
    public bool IsElevated => Environment.UserName == "root";

    public async Task<bool> BlockEntityAsync(BlockedEntity entity)
    {
        if (!IsElevated) return false;

        string command = entity.Type switch
        {
            BlockedEntityType.Application => $"iptables -A OUTPUT -m owner --cmd-owner {entity.Name} -j REJECT",
            BlockedEntityType.IpAddress => $"iptables -A OUTPUT -d {entity.Value} -j REJECT",
            _ => throw new ArgumentOutOfRangeException()
        };

        return await RunCommandAsync(command);
    }

    public async Task<bool> UnblockEntityAsync(string entityId)
    {
        // Note: For unblocking we would need to find the specific rule.
        // For the prototype, we assume the Rule name or similar mapping exists.
        // Unblocking via iptables requires the exact same command with -D.
        return true; 
    }

    public async Task<IEnumerable<BlockedEntity>> GetBlockedEntitiesAsync()
    {
        return Enumerable.Empty<BlockedEntity>();
    }

    public Task TrackConnectionsAsync(IEnumerable<ConnectionInfo> connections) => Task.CompletedTask;

    private async Task<bool> RunCommandAsync(string fullCommand)
    {
        try
        {
            var parts = fullCommand.Split(' ', 2);
            var startInfo = new ProcessStartInfo
            {
                FileName = parts[0],
                Arguments = parts.Length > 1 ? parts[1] : "",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch { return false; }
    }
}
