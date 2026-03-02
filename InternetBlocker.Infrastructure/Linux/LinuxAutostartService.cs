using System;
using System.IO;
using InternetBlocker.Core.Interfaces;

namespace InternetBlocker.Infrastructure.Linux;

public class LinuxAutostartService : IAutostartService
{
    private const string AppName = "NetBlocker";
    private string AutostartDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/autostart");
    private string DesktopFile => Path.Combine(AutostartDir, $"{AppName}.desktop");

    public bool IsEnabled => File.Exists(DesktopFile);

    public void Enable()
    {
        try
        {
            if (!Directory.Exists(AutostartDir))
                Directory.CreateDirectory(AutostartDir);

            var exePath = Environment.ProcessPath ?? "";
            var content = $"""
                [Desktop Entry]
                Type=Application
                Name={AppName}
                Exec="{exePath}"
                Terminal=false
                X-GNOME-Autostart-enabled=true
                """;

            File.WriteAllText(DesktopFile, content);
        }
        catch { }
    }

    public void Disable()
    {
        try
        {
            if (File.Exists(DesktopFile))
                File.Delete(DesktopFile);
        }
        catch { }
    }
}
