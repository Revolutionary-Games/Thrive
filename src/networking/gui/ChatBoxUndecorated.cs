using Godot;

/// <summary>
///   The visually barebones version of <see cref="ChatBox"/> containing only texts without panels.
/// </summary>
public class ChatBoxUndecorated : ChatBox
{
    private Label namePrefix = null!;
    private Container chatInput = null!;

    public override void _Ready()
    {
        base._Ready();

        chatInput = GetNode<Container>("HBoxContainer/ChatInput");
        namePrefix = chatInput.GetNode<Label>("NamePrefix");

        OnFocusExited();
    }

    protected override void OnFocusEntered()
    {
        base.OnFocusEntered();

        namePrefix.Text = $"[{NetworkManager.Instance.LocalPlayer?.Nickname}]:";

        // Preserve layout by not hiding the control completely
        chatInput.Modulate = Colors.White;
        lineEdit.MouseFilter = MouseFilterEnum.Stop;
    }

    private void OnFocusExited()
    {
        chatInput.Modulate = Colors.Transparent;
        lineEdit.MouseFilter = MouseFilterEnum.Ignore;
    }
}
