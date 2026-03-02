using System;
using Avalonia;
using InternetBlocker.Infrastructure;
using InternetBlocker.Infrastructure.Persistence;
using System.IO;

namespace InternetBlocker.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetBlocker", "settings.db");
        var repository = new SqliteBlockedEntityRepository(dbPath);
        var monitorService = ServiceFactory.CreateMonitorService();
        var firewallService = ServiceFactory.CreateFirewallService();
        var autostartService = ServiceFactory.CreateAutostartService();
        var iconService = ServiceFactory.CreateIconService();
        var notificationService = ServiceFactory.CreateNotificationService();

        var builder = BuildAvaloniaApp();
        
        builder.AfterSetup(b =>
        {
            if (b.Instance is App app)
            {
                app.MonitorService = monitorService;
                app.FirewallService = firewallService;
                app.BlockedEntityRepository = repository;
                app.AutostartService = autostartService;
                app.IconService = iconService;
                app.NotificationService = notificationService;
            }
        });

        builder.StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
