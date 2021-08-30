using Godot;

/// <summary>
///   A customized check box that changes icon when hovered / clicked.
/// </summary>
public class CustomCheckBox : ToolButton
{
    private Texture unpressedNormal;
    private Texture unpressedHover;
    private Texture unpressedClick;
    private Texture pressedNormal;
    private Texture pressedHover;
    private Texture pressedClick;

    private bool pressing;

    public override void _Ready()
    {
        unpressedNormal = GD.Load<Texture>("res://assets/textures/gui/bevel/checkA.png");
        unpressedHover = GD.Load<Texture>("res://assets/textures/gui/bevel/checkAhover.png");
        unpressedClick = GD.Load<Texture>("res://assets/textures/gui/bevel/checkAclick.png");
        pressedNormal = GD.Load<Texture>("res://assets/textures/gui/bevel/checkB.png");
        pressedHover = GD.Load<Texture>("res://assets/textures/gui/bevel/checkBhover.png");
        pressedClick = GD.Load<Texture>("res://assets/textures/gui/bevel/checkBclick.png");
    }

    public override void _Process(float delta)
    {
        if (!Visible)
            return;

        UpdateIcon();
        base._Process(delta);
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Only when button's press state changes does Godot call _Pressed(), so to show a different icon when clicked,
        // we have to capture mouse event.
        if (@event is InputEventMouseButton { ButtonIndex: (int)ButtonList.Left } mouseEvent)
        {
            pressing = mouseEvent.Pressed;
        }

        base._GuiInput(@event);
    }

    private void UpdateIcon()
    {
        if (Pressed)
        {
            if (Disabled)
            {
                Icon = pressedNormal;
            }
            else if (pressing)
            {
                Icon = pressedClick;
            }
            else
            {
                Icon = IsHovered() ? pressedHover : pressedNormal;
            }
        }
        else
        {
            if (Disabled)
            {
                Icon = unpressedNormal;
            }
            else if (pressing)
            {
                Icon = unpressedClick;
            }
            else
            {
                Icon = IsHovered() ? unpressedHover : unpressedNormal;
            }
        }
    }
}
