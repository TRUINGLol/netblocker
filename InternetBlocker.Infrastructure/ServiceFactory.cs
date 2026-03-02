using InternetBlocker.Core.Interfaces;
using InternetBlocker.Infrastructure.Windows;
using System.Runtime.InteropServices;

namespace InternetBlocker.Infrastructure;

public static class ServiceFactory
{
    public static IMonitorService CreateMonitorService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsMonitorService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new Linux.LinuxMonitorService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new Mac.MacMonitorService();
        
        throw new PlatformNotSupportedException("This platform is not yet supported.");
    }

    public static IFirewallService CreateFirewallService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsFirewallService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new Linux.LinuxFirewallService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new Mac.MacFirewallService();

        throw new PlatformNotSupportedException("This platform is not yet supported.");
    }

    public static IAutostartService CreateAutostartService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsAutostartService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new Linux.LinuxAutostartService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new Mac.MacAutostartService();

        throw new PlatformNotSupportedException("This platform is not yet supported.");
    }

    public static IIconService CreateIconService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsIconService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new Linux.LinuxIconService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new Mac.MacIconService();

        throw new PlatformNotSupportedException("This platform is not yet supported.");
    }

    public static INotificationService? CreateNotificationService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new Mac.MacNotificationService();
            
        return null;
    }
}
