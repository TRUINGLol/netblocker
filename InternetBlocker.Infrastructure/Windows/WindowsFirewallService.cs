using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;

namespace InternetBlocker.Infrastructure.Windows;

public class WindowsFirewallService : IFirewallService
{
    private const string RulePrefix = "NetBlocker_";

    public bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public async Task<bool> BlockEntityAsync(BlockedEntity entity)
    {
        if (!IsElevated) return false;

        string outboundCommand = entity.Type switch
        {
            BlockedEntityType.Application => $"advfirewall firewall add rule name=\"{entity.Id}_Out\" dir=out action=block program=\"{entity.Value}\" enable=yes",
            BlockedEntityType.IpAddress => $"advfirewall firewall add rule name=\"{entity.Value}_Out\" dir=out action=block remoteip={entity.Value} enable=yes",
            _ => throw new ArgumentOutOfRangeException()
        };

        bool outSuccess = await RunNetshAsync(outboundCommand);

        if (entity.Type == BlockedEntityType.Application)
        {
            // Also block inbound for applications to be thorough
            string inboundCommand = $"advfirewall firewall add rule name=\"{entity.Id}_In\" dir=in action=block program=\"{entity.Value}\" enable=yes";
            await RunNetshAsync(inboundCommand);
        }

        return outSuccess;
    }

    public async Task<bool> UnblockEntityAsync(string entityId)
    {
        if (!IsElevated) return false;
        await RunNetshAsync($"advfirewall firewall delete rule name=\"{entityId}_Out\"");
        await RunNetshAsync($"advfirewall firewall delete rule name=\"{entityId}_In\"");
        return true;
    }

    public async Task<IEnumerable<BlockedEntity>> GetBlockedEntitiesAsync()
    {
        // This is a bit complex via netsh as it returns text. 
        // For now, we'll assume the UI layer manages the list of "intended" blocks 
        // and we just sync them, or we could parse 'netsh show rule'.
        // For the prototype, let's keep it simple.
        return Enumerable.Empty<BlockedEntity>();
    }

    public Task TrackConnectionsAsync(IEnumerable<ConnectionInfo> connections) => Task.CompletedTask;

    private async Task<bool> RunNetshAsync(string args)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;
            
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
            {
                Debug.WriteLine($"netsh failed with exit code {process.ExitCode}");
                Debug.WriteLine($"Output: {output}");
                Debug.WriteLine($"Error: {error}");
            }
            
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception running netsh: {ex.Message}");
            return false;
        }
    }
}
