using NovaPad.Core.Enums;

namespace NovaPad.Core.Models;

public class GamepadLayout
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<ButtonType, string> ButtonLabels { get; set; } = new();
    public Dictionary<ButtonType, double> ButtonX { get; set; } = new();
    public Dictionary<ButtonType, double> ButtonY { get; set; } = new();
    public string SvgOverlayPath { get; set; } = string.Empty;

    public static GamepadLayout Xbox()
    {
        return new GamepadLayout
        {
            Name = "Xbox",
            ButtonLabels = new()
            {
                [ButtonType.A] = "A", [ButtonType.B] = "B",
                [ButtonType.X] = "X", [ButtonType.Y] = "Y"
            }
        };
    }

    public static GamepadLayout PlayStation()
    {
        return new GamepadLayout
        {
            Name = "PlayStation",
            ButtonLabels = new()
            {
                [ButtonType.A] = "Cross", [ButtonType.B] = "Circle",
                [ButtonType.X] = "Square", [ButtonType.Y] = "Triangle"
            }
        };
    }

    public static GamepadLayout Nintendo()
    {
        return new GamepadLayout
        {
            Name = "Nintendo",
            ButtonLabels = new()
            {
                [ButtonType.A] = "B", [ButtonType.B] = "A",
                [ButtonType.X] = "Y", [ButtonType.Y] = "X"
            }
        };
    }
}
