using Godot;

public class TweakedColorPickerPreset : ColorRect
{
    [Signal]
    public delegate void OnPresetSelected(Color color);

    [Signal]
    public delegate void OnPresetDeleted(TweakedColorPickerPreset preset);

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

    private void OnMouseEntered()
    {
        HintTooltip = TranslationServer.Translate("COLOR") + ": #" + Color.ToHtml() + "\n"
            + TranslationServer.Translate("LEFT_MOUSE") + ": "
            + TranslationServer.Translate("COLOR_PICKER_SELECT_PRESET") + "\n"
            + TranslationServer.Translate("RIGHT_MOUSE") + ": "
            + TranslationServer.Translate("COLOR_PICKER_DELETE_PRESET");
    }
}
