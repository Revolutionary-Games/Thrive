using Godot;

/// <summary>
///   Controls the little popup text saying "saving" and "save complete"
/// </summary>
public class SaveStatusOverlay : Control
{
    [Export]
    public NodePath StatusLabelPath;

    private static SaveStatusOverlay instance;

    private Label statusLabel;

    private float hideTimer;
    private bool hidden;

    /// <summary>
    ///   If true the next delta update is ignored to make the time to display more consistent
    /// </summary>
    private bool skipNextDelta;

    private SaveStatusOverlay()
    {
        instance = this;
    }

    public static SaveStatusOverlay Instance => instance;

    public override void _Ready()
    {
        statusLabel = GetNode<Label>(StatusLabelPath);

        Visible = false;
        hidden = true;
    }

    /// <summary>
    ///   Shows a saving related message
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="visibleTime">How long to show the message for</param>
    public void ShowMessage(string message, float visibleTime = 0.7f)
    {
        statusLabel.Text = message;
        hideTimer = visibleTime;
        ExternalSetStatus(true);
    }

    public override void _Process(float delta)
    {
        if (hideTimer > 0)
        {
            if (skipNextDelta)
            {
                skipNextDelta = false;
            }
            else
            {
                hideTimer -= delta;
            }
        }
        else
        {
            if (!hidden)
            {
                // TODO: this could do a nice fade out
                Visible = false;
                hidden = true;
            }
        }
    }

    private void ExternalSetStatus(bool visible)
    {
        Visible = visible;
        hidden = !visible;
        skipNextDelta = true;
    }
}
