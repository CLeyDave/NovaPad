using NovaPad.Core.Enums;

namespace NovaPad.Core.Models;

public class ButtonAssignment
{
    public ButtonType PhysicalButton { get; set; }
    public MappingType MappingType { get; set; } = MappingType.ToButton;

    public ButtonType TargetButton { get; set; } = ButtonType.None;
    public string TargetKey { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public ButtonActionType ActionType { get; set; } = ButtonActionType.None;
    public string ActionData { get; set; } = string.Empty;

    public bool IsModified =>
        MappingType != MappingType.ToButton ||
        (MappingType == MappingType.ToButton && TargetButton != ButtonType.None && TargetButton != PhysicalButton);

    public string DisplaySummary
    {
        get
        {
            if (!IsModified) return PhysicalButton.ToString();
            return MappingType switch
            {
                MappingType.ToButton => $"{PhysicalButton} → {TargetButton}",
                MappingType.ToKeyboard => $"{PhysicalButton} → [{TargetKey}]",
                MappingType.ToAction => $"{PhysicalButton} → {ActionName}",
                _ => PhysicalButton.ToString()
            };
        }
    }
}

public enum ButtonActionType
{
    None,
    LaunchProgram,
    RunMacro,
    OpenUrl,
    MediaPlayPause,
    MediaNext,
    MediaPrev,
    VolumeUp,
    VolumeDown,
    Mute,
    Screenshot,
    RecordClip
}
