using System;
using Godot;

/// <summary>
///   Controls the little popup text saying "saving" and "save complete"
/// </summary>
public partial class SaveStatusOverlay : Control
{
    private const string FolderMeta = "FOLDER";

    private static SaveStatusOverlay? instance;

#pragma warning disable CA2213
    [Export]
    private Label statusLabel = null!;

    [Export]
    private AnimationPlayer animationPlayer = null!;

    [Export]
    private ErrorDialog errorDialog = null!;
#pragma warning restore CA2213

    private double hideTimer;
    private bool hidden;

    /// <summary>
    ///   If true, the next delta update is ignored to make the time to display more consistent
    /// </summary>
    private bool skipNextDelta;

    private SaveStatusOverlay()
    {
        instance = this;
    }

    public static SaveStatusOverlay Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Ready()
    {
        Visible = false;
        hidden = true;
    }

    public override void _Process(double delta)
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
                animationPlayer.Play("SavingStatusFadeOut");
                hidden = true;
            }
        }
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
    /// <param name="wasLoading">
    ///   Set to true when the problem was during loading (the error is shown slightly differently)
    /// </param>
    /// <param name="returnToMenu">
    ///   If true, closing the dialog returns to the menu.
    ///   If false, the dialog is just closed (and the game is unpaused)
    /// </param>
    /// <param name="onClosed">Callback for when the dialog is closed</param>
    /// <param name="allowExceptionCopy">
    ///   If true allows the user to copy the error, should be on if exception is an exception
    /// </param>
    public void ShowError(string title, string message, string exception, bool wasLoading, bool returnToMenu = false,
        Action? onClosed = null, bool allowExceptionCopy = true)
    {
        errorDialog.ShowError(title, message, exception, returnToMenu, onClosed, allowExceptionCopy);
    }

    private void ExternalSetStatus(bool visible)
    {
        Visible = visible;
        hidden = !visible;
        skipNextDelta = true;
    }

    private void OpenLogsFolder()
    {
        GD.Print("Clicked on open logs folder, trying to open it");
        FolderHelpers.OpenFolder(Constants.LOGS_FOLDER);
    }

    // TODO: this is probably unused now but might be good to add a new way to open the logs folder?
    private void DebugAdviceMetaClicked(string meta)
    {
        if (meta == FolderMeta)
        {
            OpenLogsFolder();
        }
        else
        {
            GD.PrintErr("Unknown meta clicked in save error JSON debug advice label: ", meta);
        }
    }
}
