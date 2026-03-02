using System.Threading.Tasks;

namespace InternetBlocker.Core.Interfaces;

public interface INotificationService
{
    Task ShowNotificationAsync(string title, string message, string? actionLabel = null, Action? onAction = null);
}
