namespace NovaPad.Core.Events;

public class WidgetToggleEvent
{
    public string WidgetId { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
}
