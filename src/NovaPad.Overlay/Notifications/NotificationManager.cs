using System.Windows;
using System.Windows.Controls;
using NovaPad.Core.Events;

namespace NovaPad.Overlay.Notifications;

public class NotificationManager
{
    private readonly StackPanel _container;
    private readonly Queue<NotificationData> _pendingQueue = new();
    private readonly List<NotificationControl> _activeNotifications = new();
    private readonly List<NotificationControl> _pool = new();
    private const int MaxVisible = 5;
    private const int PoolSize = 8;
    private const double DismissAfterSeconds = 3.0;

    public NotificationManager(StackPanel container)
    {
        _container = container;
        PreallocatePool();
    }

    private void PreallocatePool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var ctrl = new NotificationControl();
            ctrl.Visibility = Visibility.Collapsed;
            _container.Children.Add(ctrl);
            _pool.Add(ctrl);
        }
    }

    public void ShowNotification(NotificationData data)
    {
        if (_activeNotifications.Count >= MaxVisible)
        {
            _pendingQueue.Enqueue(data);
            return;
        }

        var ctrl = AcquireFromPool();
        if (ctrl == null) return;

        ctrl.Bind(data);
        ctrl.Visibility = Visibility.Visible;
        ctrl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ctrl.Arrange(new Rect(ctrl.DesiredSize));
        _activeNotifications.Add(ctrl);

        _container.Children.Remove(ctrl);
        _container.Children.Insert(0, ctrl);

        _ = ScheduleDismiss(ctrl);
    }

    private async Task ScheduleDismiss(NotificationControl ctrl)
    {
        await Task.Delay(TimeSpan.FromSeconds(DismissAfterSeconds));
        if (ctrl.Visibility == Visibility.Visible)
        {
            DismissNotification(ctrl);
        }
    }

    public void DismissNotification(NotificationControl ctrl)
    {
        ctrl.Dismiss();
        _ = ReturnToPoolAfterDismiss(ctrl);
    }

    private async Task ReturnToPoolAfterDismiss(NotificationControl ctrl)
    {
        await Task.Delay(300);
        _activeNotifications.Remove(ctrl);
        ReturnToPool(ctrl);
        ProcessQueue();
    }

    private NotificationControl? AcquireFromPool()
    {
        var ctrl = _pool.FirstOrDefault(c => c.Visibility != Visibility.Visible);
        return ctrl;
    }

    private void ReturnToPool(NotificationControl ctrl)
    {
        ctrl.Visibility = Visibility.Collapsed;
        _container.Children.Remove(ctrl);
        _container.Children.Add(ctrl);
    }

    private void ProcessQueue()
    {
        while (_pendingQueue.Count > 0 && _activeNotifications.Count < MaxVisible)
        {
            var next = _pendingQueue.Dequeue();
            ShowNotification(next);
        }
    }

    public void ClearAll()
    {
        foreach (var ctrl in _activeNotifications.ToList())
            DismissNotification(ctrl);
        _pendingQueue.Clear();
    }
}
