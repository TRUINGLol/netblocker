using System.Diagnostics;
using Microsoft.Win32;
using InternetBlocker.Core.Interfaces;

namespace InternetBlocker.Infrastructure.Windows;

public class WindowsAutostartService : IAutostartService
{
    private const string AppName = "NetBlocker";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(AppName) != null;
        }
    }

    public void Enable()
    {
        var path = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(path)) return;

        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        key?.SetValue(AppName, $"\"{path}\"");
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        key?.DeleteValue(AppName, false);
    }
}
