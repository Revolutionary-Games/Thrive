using System;
using Godot;

/// <summary>
///   A dialog popup dedicated for showing error and Exception messages.
/// </summary>
/// TODO: see https://github.com/Revolutionary-Games/Thrive/issues/2751
/// [Tool]
public class ErrorDialog : CustomDialog
{
    private string errorMessage;
    private string exceptionInfo;

    /// <summary>
    ///   If true closing the dialog returns to menu. If false the dialog is just closed (and game is unpaused).
    /// </summary>
    private bool onDismissReturnToMenu;

    /// <summary>
    ///   Callback for when the dialog is closed.
    /// </summary>
    private Action onCloseCallback;

    private Label extraDescriptionLabel;
    private Label exceptionLabel;
    private VBoxContainer exceptionBox;
    private Control copyException;

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
    public string ExceptionInfo
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
        Action onClosed = null, bool allowExceptionCopy = true)
    {
        WindowTitle = title;
        ErrorMessage = message;
        ExceptionInfo = exception;
        copyException.Visible = allowExceptionCopy;
        this.PopupCenteredShrink();

        onDismissReturnToMenu = returnToMenu;
        onCloseCallback = onClosed;
    }

    private void UpdateMessage()
    {
        extraDescriptionLabel.SizeFlagsVertical = exceptionBox.Visible ?
            (int)SizeFlags.Fill :
            (int)SizeFlags.ExpandFill;
        extraDescriptionLabel.Text = errorMessage;
    }

    private void UpdateException()
    {
        exceptionLabel.Text = exceptionInfo;
        exceptionBox.Visible = !string.IsNullOrEmpty(exceptionInfo);
    }

    private void OnErrorDialogDismissed()
    {
        SceneManager.Instance.GetTree().Paused = false;

        if (onDismissReturnToMenu)
        {
            SceneManager.Instance.ReturnToMenu();
        }

        onCloseCallback?.Invoke();
    }

    private void OnCopyToClipboardPressed()
    {
        OS.Clipboard = TranslationServer.Translate(WindowTitle) + " - " +
            TranslationServer.Translate(extraDescriptionLabel.Text) + " exception: " +
            exceptionLabel.Text;
    }

    private void OnClosePressed()
    {
        Hide();
    }
}
