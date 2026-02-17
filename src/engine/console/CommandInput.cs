using Godot;

/// <summary>
///   A custom LineEdit to handle command inputs.
/// </summary>
public partial class CommandInput : LineEdit
{
    private bool inputKeyPressed;

    public CommandHistory? CommandHistory { get; set; }

    public override void _GuiInput(InputEvent @event)
    {
        if (CommandHistory is null)
            return;

        if (CommandHistory.HistoryLength == 0)
            return;

        if (@event is not InputEventKey keyEvent)
            return;

        if (inputKeyPressed)
        {
            if (!keyEvent.Pressed)
                inputKeyPressed = false;

            return;
        }

        switch (keyEvent.Keycode)
        {
            case Key.Up:
                if (!CommandHistory.LookingUp)
                    CommandHistory.LookingUp = true;

                ++CommandHistory.CommandHistoryIndex;
                break;
            case Key.Down:
                if (!CommandHistory.LookingUp)
                    return;

                --CommandHistory.CommandHistoryIndex;
                break;
            default:
                return;
        }

        inputKeyPressed = true;

        SetText(CommandHistory.CurrentCommand);
        AcceptEvent();

        CaretColumn = Text.Length;

        base._GuiInput(@event);
    }

    private void OnInput(string command)
    {
        _ = command;

        // Ensure we are not looking up the history anymore after executing a command.
        CommandHistory?.LookingUp = false;

        Clear();
    }
}
