using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;

namespace InternetBlocker.Infrastructure.Mac;

public class MacFirewallService : IFirewallService
{
    private const string AnchorName = "com.netblocker.rules";
    private const string RulesFilePath = "/tmp/netblocker.pf.conf";
    private const string TableName = "blocked_ips";

    public bool IsElevated => Environment.UserName == "root";

    public async Task<bool> BlockEntityAsync(BlockedEntity entity)
    {
        if (!IsElevated) return false;

        try
        {
            if (entity.Type == BlockedEntityType.IpAddress)
            {
                return await AddIpToTableAsync(entity.Value);
            }

            // For Applications, we record the intended block in our rules file as a comment
            // and use the Monitor to drive reactive IP blocking.
            var rules = await GetCurrentRulesInternalAsync();
            string marker = $"# BlockApp:{entity.Value} id:{entity.Id}";
            
            if (rules.Any(r => r.Contains(entity.Id))) return true;
            
            rules.Add(marker);
            return await ApplyRulesAsync(rules);
        }
        catch { return false; }
    }

    private async Task<bool> AddIpToTableAsync(string ip)
    {
        if (string.IsNullOrEmpty(ip) || ip == "*" || ip == "0.0.0.0") return true;

        var startInfo = new ProcessStartInfo
        {
            FileName = "pfctl",
            Arguments = $"-t {TableName} -T add {ip}",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo);
        if (process == null) return false;
        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    public async Task<bool> UnblockEntityAsync(string entityId)
    {
        if (!IsElevated) return false;

        try
        {
            var rules = await GetCurrentRulesInternalAsync();
            var toRemove = rules.Where(r => r.Contains($"id:{entityId}")).ToList();
            
            foreach (var r in toRemove) rules.Remove(r);

            // If we unblock an app, we should probably flush the IP table too?
            // To be safe, we'll flush it if anything is unblocked.
            if (toRemove.Count > 0)
            {
                Process.Start(new ProcessStartInfo { FileName = "pfctl", Arguments = $"-t {TableName} -T flush", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit();
                return await ApplyRulesAsync(rules);
            }
            return true;
        }
        catch { return false; }
    }

    public async Task<IEnumerable<BlockedEntity>> GetBlockedEntitiesAsync()
    {
        return Enumerable.Empty<BlockedEntity>();
    }

    private async Task<List<string>> GetCurrentRulesInternalAsync()
    {
        if (!File.Exists(RulesFilePath)) return new List<string>();
        return (await File.ReadAllLinesAsync(RulesFilePath)).ToList();
    }

    private async Task<bool> ApplyRulesAsync(List<string> rules)
    {
        try
        {
            // Ensure core rules are present
            string tableDef = $"table <{TableName}> persist";
            string blockRule = $"block out from any to <{TableName}>";

            if (!rules.Any(r => r.Contains($"table <{TableName}>"))) rules.Insert(0, tableDef);
            if (!rules.Any(r => r.Contains($"to <{TableName}>"))) rules.Add(blockRule);

            await File.WriteAllLinesAsync(RulesFilePath, rules);
            
            // Enable PF
            Process.Start(new ProcessStartInfo { FileName = "pfctl", Arguments = "-e", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit();

            // Load to anchor
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "pfctl",
                Arguments = $"-a {AnchorName} -f {RulesFilePath}",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) return false;
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch { return false; }
    }

    public Task TrackConnectionsAsync(IEnumerable<ConnectionInfo> connections) => Task.CompletedTask;
}
