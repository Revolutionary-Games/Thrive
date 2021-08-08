using System.Globalization;
using Godot;

public class TweakedColorPickerPreset : ColorRect
{
    [Signal]
    public delegate void OnPresetSelected(Color color);

    [Signal]
    public delegate void OnPresetDeleted(TweakedColorPickerPreset preset);

    public static string HintTooltipBase { get; set; }

    public override void _Ready()
    {
        Translate();
        base._Ready();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            Translate();

        base._Notification(what);
    }

    private void OnPresetGUIInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true } mouseEvent)
        {
            switch ((ButtonList)mouseEvent.ButtonIndex)
            {
                case ButtonList.Left:
                    EmitSignal(nameof(OnPresetSelected), Color);
                    break;
                case ButtonList.Right:
                    EmitSignal(nameof(OnPresetDeleted), this);
                    break;
            }
        }
    }

    private void Translate()
    {
        HintTooltip = string.Format(CultureInfo.CurrentCulture, HintTooltipBase, Color.ToHtml());
    }
}
