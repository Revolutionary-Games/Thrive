using System.Text;
using Godot;

/// <summary>
///   A simple chatting control for multiplayer, fully decorated (with panels).
/// </summary>
public class ChatBox : VBoxContainer
{
    [Export]
    public NodePath MessagesPath = null!;

    [Export]
    public NodePath LineEditPath = null!;

    [Export]
    public NodePath SendButtonPath = null!;

    protected LineEdit lineEdit = null!;

    private CustomRichTextLabel chatDisplay = null!;
    private Button sendButton = null!;

    private bool controlsHoveredOver;

    [Signal]
    public delegate void Focused();

    [Export]
    public bool ReleaseLineEditFocusAfterMessageSent { get; set; } = true;

    [Export]
    public bool CaptureFocusWhenInvisible { get; set; } = true;

    public override void _EnterTree()
    {
        InputManager.RegisterReceiver(this);
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        base._ExitTree();
    }

    public override void _Ready()
    {
        chatDisplay = GetNode<CustomRichTextLabel>(MessagesPath);
        lineEdit = GetNode<LineEdit>(LineEditPath);
        sendButton = GetNode<Button>(SendButtonPath);

        NetworkManager.Instance.Connect(nameof(NetworkManager.ChatReceived), this, nameof(OnMessageReceived));

        OnMessageChanged(string.Empty);
        DisplayChat();
    }

    [RunOnKeyDown("g_focus_chat")]
    public bool Focus()
    {
        if (!CaptureFocusWhenInvisible && !Visible)
            return false;

        lineEdit.GrabFocus();
        return true;
    }

    [RunOnKeyDown("g_chat_command")]
    public bool FocusCommand()
    {
        if (!Focus())
            return false;

        lineEdit.Text = "/";
        lineEdit.CaretPosition = 1;
        return true;
    }

    protected virtual void OnFocusEntered()
    {
        EmitSignal(nameof(Focused));
    }

    protected virtual void OnMessageEntered(string message)
    {
        if (ReleaseLineEditFocusAfterMessageSent)
            lineEdit.ReleaseFocus();

        if (string.IsNullOrEmpty(message))
            return;

        lineEdit.Text = string.Empty;

        OnMessageChanged(lineEdit.Text);
        NetworkManager.Instance.Chat(message);
    }

    protected virtual void OnMessageChanged(string newText)
    {
        sendButton.Disabled = string.IsNullOrEmpty(newText);
    }

    private void DisplayChat()
    {
        var builder = new StringBuilder(100);

        var first = true;
        foreach (var message in NetworkManager.Instance.ChatHistory)
        {
            if (!first)
                builder.Append('\n');

            builder.Append(message);
            first = false;
        }

        chatDisplay.ExtendedBbcode = builder.ToString();
    }

    private void OnMessageReceived()
    {
        DisplayChat();
    }

    private void OnSendPressed()
    {
        OnMessageEntered(lineEdit.Text);
    }

    private void OnClickedOffTextBox()
    {
        var focused = GetFocusOwner();

        // Ignore if the species name line edit wasn't focused or if one of our controls is hovered
        if (focused != lineEdit || controlsHoveredOver)
            return;

        lineEdit.ReleaseFocus();
    }

    private void OnControlMouseEntered()
    {
        controlsHoveredOver = true;
    }

    private void OnControlMouseExited()
    {
        controlsHoveredOver = false;
    }
}
