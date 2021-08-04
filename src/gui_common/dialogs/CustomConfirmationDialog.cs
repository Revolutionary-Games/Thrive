using Godot;

/// <summary>
///   A custom reimplementation of ConfirmationDialog and AcceptDialog combined into one.
/// </summary>
[Tool]
public class CustomConfirmationDialog : CustomDialog
{
    [Export]
    public bool HideOnOk = true;

    private bool hideCancelButton;

    private string dialogText;
    private string confirmText = "OK";
    private string cancelText = "CANCEL";

    private Label dialogLabel;
    private Button confirmButton;
    private Button cancelButton;
    private Control cancelEndSpacer;

    [Signal]
    public delegate void Confirmed();

    [Export]
    public bool HideCancelButton
    {
        get => hideCancelButton;
        set
        {
            hideCancelButton = value;

            if (cancelButton != null)
                UpdateButtons();
        }
    }

    /// <summary>
    ///   The text displayed by the dialog.
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string DialogText
    {
        get => dialogText;
        set
        {
            dialogText = value;

            if (dialogLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   The text to be shown on the confirm button.
    /// </summary>
    [Export]
    public string ConfirmText
    {
        get => confirmText;
        set
        {
            confirmText = value;

            if (confirmButton != null)
                UpdateButtons();
        }
    }

    /// <summary>
    ///   The text to be shown on the cancel button.
    /// </summary>
    [Export]
    public string CancelText
    {
        get => cancelText;
        set
        {
            cancelText = value;

            if (cancelButton != null)
                UpdateButtons();
        }
    }

    public override void _Ready()
    {
        dialogLabel = GetNode<Label>("VBoxContainer/Label");
        confirmButton = GetNode<Button>("VBoxContainer/VBoxContainer/ConfirmButton");
        cancelButton = GetNode<Button>("VBoxContainer/VBoxContainer/CancelButton");
        cancelEndSpacer = GetNode<Control>("VBoxContainer/VBoxContainer/Spacer3");

        UpdateLabel();
        UpdateButtons();
    }

    private void UpdateLabel()
    {
        dialogLabel.Text = TranslationServer.Translate(dialogText);
    }

    private void UpdateButtons()
    {
        cancelButton.Visible = !hideCancelButton;
        cancelEndSpacer.Visible = !hideCancelButton;

        confirmButton.Text = TranslationServer.Translate(confirmText);
        cancelButton.Text = TranslationServer.Translate(cancelText);
    }

    private void OnConfirmPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(Confirmed));

        if (HideOnOk)
            ClosePopup();
    }

    private void OnCancelPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        ClosePopup();
    }
}
