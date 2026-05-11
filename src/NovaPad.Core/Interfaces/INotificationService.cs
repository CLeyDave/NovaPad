using NovaPad.Core.Models;

namespace NovaPad.Core.Interfaces;

public interface INotificationService
{
    event EventHandler<NotificationMessage>? NotificationReceived;

    void Show(NotificationMessage notification);
    void Show(string title, string message, NotificationType type = NotificationType.Info, TimeSpan? duration = null);
    void Dismiss(string notificationId);
    void DismissAll();
    IReadOnlyList<NotificationMessage> ActiveNotifications { get; }
}
