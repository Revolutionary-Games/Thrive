using System;
using Godot;

/// <summary>
///   A dialog popup dedicated for showing error and Exception messages.
/// </summary>
/// TODO: see https://github.com/Revolutionary-Games/Thrive/issues/2751
/// [Tool]
public class ErrorDialog : CustomDialog
{
    private string errorMessage = string.Empty;
    private string? exceptionInfo;

    /// <summary>
    ///   If true closing the dialog returns to menu. If false the dialog is just closed (and game is unpaused).
    /// </summary>
    private bool onDismissReturnToMenu;

    /// <summary>
    ///   Callback for when the dialog is closed.
    /// </summary>
    private Action? onCloseCallback;

#pragma warning disable CA2213
    private Label? extraDescriptionLabel;
    private Label? exceptionLabel;
    private VBoxContainer exceptionBox = null!;
    private Control copyException = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   The main error message.
    /// </summary>
    [Export]
    public string ErrorMessage
    {
        get => errorMessage;
        set
        {
            errorMessage = value;

            if (extraDescriptionLabel != null)
                UpdateMessage();
        }
    }

    /// <summary>
    ///   The additional exception info that is thrown from an error.
    /// </summary>
    [Export]
    public string? ExceptionInfo
    {
        get => exceptionInfo;
        set
        {
            exceptionInfo = value;

            if (exceptionLabel != null || extraDescriptionLabel != null)
            {
                UpdateException();
                UpdateMessage();
            }
        }
    }

    private string PauseLock => $"{nameof(ErrorDialog)}_{Name}";

    public override void _Ready()
    {
        extraDescriptionLabel = GetNode<Label>("VBoxContainer/ExtraErrorDescription");
        exceptionLabel = GetNode<Label>(
            "VBoxContainer/ExceptionBox/PanelContainer/MarginContainer/ScrollContainer/Exception");
        exceptionBox = GetNode<VBoxContainer>("VBoxContainer/ExceptionBox");
        copyException = GetNode<Control>("VBoxContainer/ExceptionBox/CopyErrorButton");

        UpdateMessage();
        UpdateException();
    }

    /// <summary>
    ///   Helper for showing the error dialog with extra callback.
    /// </summary>
    public void ShowError(string title, string message, string exception, bool returnToMenu = false,
        Action? onClosed = null, bool allowExceptionCopy = true)
    {
        WindowTitle = title;
        ErrorMessage = message;
        ExceptionInfo = exception;
        copyException.Visible = allowExceptionCopy;
        PopupCenteredShrink();

        onDismissReturnToMenu = returnToMenu;
        onCloseCallback = onClosed;

        PauseManager.Instance.AddPause(PauseLock);
    }

    protected override void OnHidden()
    {
        base.OnHidden();
        OnErrorDialogDismissed();
    }

    private void UpdateMessage()
    {
        extraDescriptionLabel!.SizeFlagsVertical = exceptionBox.Visible ?
            (int)SizeFlags.Fill :
            (int)SizeFlags.ExpandFill;
        extraDescriptionLabel.Text = errorMessage;
    }

    private void UpdateException()
    {
        exceptionLabel!.Text = exceptionInfo;
        exceptionBox.Visible = !string.IsNullOrEmpty(exceptionInfo);
    }

    private void OnErrorDialogDismissed()
    {
        PauseManager.Instance.Resume(PauseLock);

        if (onDismissReturnToMenu)
        {
            SceneManager.Instance.ReturnToMenu();
        }

        onCloseCallback?.Invoke();
    }

    private void OnCopyToClipboardPressed()
    {
        OS.Clipboard = TranslationServer.Translate(WindowTitle) + " - " +
            TranslationServer.Translate(extraDescriptionLabel!.Text) + " exception: " +
            exceptionLabel!.Text;
    }

    private void OnClosePressed()
    {
        Hide();
    }
}
