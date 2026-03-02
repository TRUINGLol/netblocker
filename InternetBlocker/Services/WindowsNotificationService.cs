using System;
using System.Threading.Tasks;
using InternetBlocker.Core.Interfaces;
using Avalonia.Controls.Notifications;
using Avalonia.Controls;

namespace InternetBlocker.Services;

public class WindowsNotificationService : INotificationService
{
    private readonly Window _parentWindow;
    private WindowNotificationManager? _notificationManager;

    public WindowsNotificationService(Window parentWindow)
    {
        _parentWindow = parentWindow;
    }

    public Task ShowNotificationAsync(string title, string message, string? actionLabel = null, Action? onAction = null)
    {
        if (_notificationManager == null)
        {
            _notificationManager = new WindowNotificationManager(_parentWindow)
            {
                Position = NotificationPosition.BottomRight,
                MaxItems = 3
            };
        }

        var notification = new Notification(title, message, NotificationType.Information, TimeSpan.FromSeconds(5), onClick: onAction);
        
        _notificationManager.Show(notification);

        return Task.CompletedTask;
    }
}
