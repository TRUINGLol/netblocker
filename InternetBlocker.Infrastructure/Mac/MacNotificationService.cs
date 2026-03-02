using System;
using System.Diagnostics;
using System.Threading.Tasks;
using InternetBlocker.Core.Interfaces;

namespace InternetBlocker.Infrastructure.Mac;

public class MacNotificationService : INotificationService
{
    public Task ShowNotificationAsync(string title, string message, string? actionLabel = null, Action? onAction = null)
    {
        // actionLabel and onAction are not easily supported via simple osascript without more complex code
        // For now, let's do a simple system notification.
        try
        {
            var escapedTitle = title.Replace("\"", "\\\"");
            var escapedMessage = message.Replace("\"", "\\\"");
            
            // macOS system notification via AppleScript
            var script = $"display notification \"{escapedMessage}\" with title \"{escapedTitle}\"";
            
            Process.Start(new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e '{script}'",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { }
        
        return Task.CompletedTask;
    }
}
