using System.Collections.ObjectModel;
using System.Windows;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.Services;

public class NotificationService : INotificationService
{
    public event EventHandler<NotificationMessage>? NotificationReceived;

    public IReadOnlyList<NotificationMessage> ActiveNotifications => _notifications.AsReadOnly();
    private readonly ObservableCollection<NotificationMessage> _notifications = new();

    public void Show(NotificationMessage notification)
    {
        _notifications.Add(notification);
        NotificationReceived?.Invoke(this, notification);

        if (notification.Duration > TimeSpan.Zero)
        {
            _ = DismissAfterDelay(notification.Id, notification.Duration);
        }
    }

    public void Show(string title, string message, NotificationType type = NotificationType.Info, TimeSpan? duration = null)
    {
        Show(new NotificationMessage
        {
            Title = title,
            Message = message,
            Type = type,
            Duration = duration ?? TimeSpan.FromSeconds(5)
        });
    }

    public void Dismiss(string notificationId)
    {
        if (Application.Current?.Dispatcher.CheckAccess() == false)
        {
            Application.Current.Dispatcher.Invoke(() => Dismiss(notificationId));
            return;
        }

        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsDismissed = true;
            _notifications.Remove(notification);
        }
    }

    public void DismissAll()
    {
        if (Application.Current?.Dispatcher.CheckAccess() == false)
        {
            Application.Current.Dispatcher.Invoke(DismissAll);
            return;
        }

        foreach (var n in _notifications.ToList())
        {
            n.IsDismissed = true;
            _notifications.Remove(n);
        }
    }

    private async Task DismissAfterDelay(string notificationId, TimeSpan delay)
    {
        await Task.Delay(delay);
        Dismiss(notificationId);
    }
}
