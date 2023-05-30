using System;
using Godot;
using Saving;

/// <summary>
///   Controls the little popup text saying "saving" and "save complete"
/// </summary>
public class SaveStatusOverlay : Control
{
    [Export]
    public NodePath? StatusLabelPath;

    [Export]
    public NodePath AnimationPlayerPath = null!;

    [Export]
    public NodePath ErrorDialogPath = null!;

    [Export]
    public NodePath ErrorJsonDebugAdvicePath = null!;

    [Export]
    public NodePath ErrorJsonDebugLabelPath = null!;

    private const string DebugMeta = "DEBUG";
    private const string FolderMeta = "FOLDER";

    private static SaveStatusOverlay? instance;

#pragma warning disable CA2213
    private Label statusLabel = null!;
    private AnimationPlayer animationPlayer = null!;

    private ErrorDialog errorDialog = null!;
    private Control errorJsonDebugAdvice = null!;
    private CustomRichTextLabel errorJsonDebugLabel = null!;
#pragma warning restore CA2213

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

    public static SaveStatusOverlay Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Ready()
    {
        statusLabel = GetNode<Label>(StatusLabelPath);
        animationPlayer = GetNode<AnimationPlayer>(AnimationPlayerPath);

        errorDialog = GetNode<ErrorDialog>(ErrorDialogPath);
        errorJsonDebugAdvice = GetNode<Control>(ErrorJsonDebugAdvicePath);
        errorJsonDebugLabel = GetNode<CustomRichTextLabel>(ErrorJsonDebugLabelPath);

        Visible = false;
        hidden = true;
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
    ///   If true closing the dialog returns to menu. If false the dialog is just closed (and game is unpaused)
    /// </param>
    /// <param name="onClosed">Callback for when the dialog is closed</param>
    /// <param name="allowExceptionCopy">
    ///   If true allows the user to copy the error, should be on if exception is an exception
    /// </param>
    public void ShowError(string title, string message, string exception, bool wasLoading, bool returnToMenu = false,
        Action? onClosed = null, bool allowExceptionCopy = true)
    {
        // When explicitly failing with a message, don't want to show the advice as that text always does fully explain
        // what is wrong
        if (!wasLoading && !string.IsNullOrWhiteSpace(exception) && allowExceptionCopy)
        {
            errorJsonDebugAdvice.Visible = true;

            if (JsonDebugFileExists() && Settings.Instance.JSONDebugMode != JSONDebug.DebugMode.AlwaysDisabled)
            {
                SetJsonDebugLabelText();
            }
            else
            {
                SetJsonDebugLabelMissingFileText();
            }
        }
        else
        {
            errorJsonDebugAdvice.Visible = false;
        }

        // TODO: could the exception have the last few lines from the json debug log included?
        // That way users would always have the critical context for a saving error

        errorDialog.ShowError(title, message, exception, returnToMenu, onClosed, allowExceptionCopy);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (StatusLabelPath != null)
            {
                StatusLabelPath.Dispose();
                AnimationPlayerPath.Dispose();
                ErrorDialogPath.Dispose();
                ErrorJsonDebugAdvicePath.Dispose();
                ErrorJsonDebugLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ExternalSetStatus(bool visible)
    {
        Visible = visible;
        hidden = !visible;
        skipNextDelta = true;
    }

    private void SetJsonDebugLabelText()
    {
        errorJsonDebugLabel.ExtendedBbcode =
            TranslationServer.Translate("SAVE_ERROR_INCLUDE_JSON_DEBUG_NOTE")
                .FormatSafe(DebugMeta, Constants.JSON_DEBUG_OUTPUT_FILE_NAME, FolderMeta);
    }

    private void SetJsonDebugLabelMissingFileText()
    {
        errorJsonDebugLabel.ExtendedBbcode =
            TranslationServer.Translate("SAVE_ERROR_TURN_ON_JSON_DEBUG_MODE")
                .FormatSafe(Constants.JSON_DEBUG_OUTPUT_FILE_NAME, FolderMeta);
    }

    private bool JsonDebugFileExists()
    {
        using var directory = new Directory();
        return directory.FileExists(Constants.JSON_DEBUG_OUTPUT_FILE);
    }

    private void OpenJsonDebugFile()
    {
        GD.Print("Clicked on json debug file, trying to open it");
        FolderHelpers.OpenFile(Constants.JSON_DEBUG_OUTPUT_FILE);
    }

    private void OpenLogsFolder()
    {
        GD.Print("Clicked on open logs folder, trying to open it");
        FolderHelpers.OpenFolder(Constants.LOGS_FOLDER);
    }

    private void DebugAdviceMetaClicked(string meta)
    {
        if (meta == DebugMeta)
        {
            OpenJsonDebugFile();
        }
        else if (meta == FolderMeta)
        {
            OpenLogsFolder();
        }
        else
        {
            GD.PrintErr("Unknown meta clicked in save error JSON debug advice label: ", meta);
        }
    }
}
