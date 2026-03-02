using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using InternetBlocker.ViewModels;
using InternetBlocker.Views;
using InternetBlocker.Core.Interfaces;

namespace InternetBlocker;

public partial class App : Application
{
    public IMonitorService? MonitorService { get; set; }
    public IFirewallService? FirewallService { get; set; }
    public IBlockedEntityRepository? BlockedEntityRepository { get; set; }
    public IAutostartService? AutostartService { get; set; }
    public IIconService? IconService { get; set; }
    public INotificationService? NotificationService { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (MonitorService == null || FirewallService == null || BlockedEntityRepository == null || 
            AutostartService == null || IconService == null)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            
            // Use native notification service if provided, otherwise fallback to Avalonia-based one
            var effectiveNotificationService = NotificationService ?? new InternetBlocker.Services.WindowsNotificationService(mainWindow);
            
            var mainVm = new MainViewModel(MonitorService, FirewallService, BlockedEntityRepository, AutostartService, IconService, effectiveNotificationService);
            
            mainWindow.DataContext = mainVm;
            desktop.MainWindow = mainWindow;
            
            DisableAvaloniaDataAnnotationValidation();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var mainVm = new MainViewModel(MonitorService, FirewallService, BlockedEntityRepository, AutostartService, IconService, null!);
            singleViewPlatform.MainView = new MainView
            {
                DataContext = mainVm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TrayIcon_OnShowClick(object sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Show();
            desktop.MainWindow?.Activate();
        }
    }

    private void TrayIcon_OnExitClick(object sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}