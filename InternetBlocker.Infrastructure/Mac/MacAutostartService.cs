using System;
using System.Diagnostics;
using System.IO;
using InternetBlocker.Core.Interfaces;

namespace InternetBlocker.Infrastructure.Mac;

public class MacAutostartService : IAutostartService
{
    private readonly string _plistPath;
    private readonly string _label = "com.netblocker.agent";

    public MacAutostartService()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _plistPath = Path.Combine(home, "Library", "LaunchAgents", $"{_label}.plist");
    }

    public bool IsEnabled => File.Exists(_plistPath);

    public void Enable()
    {
        try
        {
            var directory = Path.GetDirectoryName(_plistPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            // If we are running inside a .app bundle, we want the .app to start, not just the binary
            // But for now, the binary is fine as it's what we have.
            
            var plistContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>{_label}</string>
    <key>ProgramArguments</key>
    <array>
        <string>{exePath}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <false/>
</dict>
</plist>";
            File.WriteAllText(_plistPath, plistContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enabling autostart: {ex.Message}");
        }
    }

    public void Disable()
    {
        try
        {
            if (File.Exists(_plistPath))
            {
                File.Delete(_plistPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disabling autostart: {ex.Message}");
        }
    }
}
