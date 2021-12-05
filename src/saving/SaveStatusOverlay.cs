using System;
using Godot;

/// <summary>
///   Controls the little popup text saying "saving" and "save complete"
/// </summary>
public class SaveStatusOverlay : Control
{
    [Export]
    public NodePath StatusLabelPath;

    [Export]
    public NodePath AnimationPlayerPath;

    [Export]
    public NodePath ErrorDialogPath;

    private static SaveStatusOverlay instance;

    private Label statusLabel;
    private AnimationPlayer animationPlayer;

    private ErrorDialog errorDialog;

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
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);
        errorDialog = GetNode<ErrorDialog>(ErrorDialogPath);

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
        statusLabel.Modulate = new Color(1, 1, 1, 1);
        statusLabel.Text = message;
        hideTimer = visibleTime;
        ExternalSetStatus(true);
    }

    /// <summary>
    ///   Shows an error dialog
    /// </summary>
    /// <param name="title">Title of the dialog to show</param>
    /// <param name="message">Message to show</param>
    /// <param name="exception">Extra / exception info to show</param>
    /// <param name="returnToMenu">
    ///   If true closing the dialog returns to menu. If false the dialog is just closed (and game is unpaused)
    /// </param>
    /// <param name="onClosed">Callback for when the dialog is closed</param>
    /// <param name="allowExceptionCopy">
    ///   If true allows the user to copy the error, should be on if exception is an exception
    /// </param>
    public void ShowError(string title, string message, string exception, bool returnToMenu = false,
        Action onClosed = null, bool allowExceptionCopy = true)
    {
        errorDialog.ShowError(title, message, exception, returnToMenu, onClosed, allowExceptionCopy);
    }

    public override void _Process(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

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
                animationPlayer.Play("SavingStatusFadeOut");
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
