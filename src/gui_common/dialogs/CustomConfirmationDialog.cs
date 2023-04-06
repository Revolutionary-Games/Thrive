using Godot;

/// <summary>
///   A custom reimplementation of ConfirmationDialog and AcceptDialog combined into one.
/// </summary>
/// TODO: see https://github.com/Revolutionary-Games/Thrive/issues/2751
/// [Tool]
public class CustomConfirmationDialog : CustomDialog
{
    [Export]
    public bool HideOnOk = true;

    [Export]
    public bool CenterText = true;

    private bool hideCancelButton;

    private string dialogText = string.Empty;
    private string confirmText = "OK";
    private string cancelText = "CANCEL";

#pragma warning disable CA2213
    private CustomRichTextLabel? dialogLabel;
    private HBoxContainer buttonsContainer = null!;
    private Button? confirmButton;
    private Button? cancelButton;
    private Control cancelEndSpacer = null!;
    private FocusGrabber? focusGrabber;
#pragma warning restore CA2213

    /// <summary>
    ///   Emitted when OK button is pressed. For Cancel see <see cref="CustomDialog.Canceled"/>.
    /// </summary>
    [Signal]
    public delegate void Confirmed();

    /// <summary>
    ///   If true, turns this dialog into its AcceptDialog form (only Ok button visible).
    /// </summary>
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
        dialogLabel = GetNode<CustomRichTextLabel>("VBoxContainer/Label");
        buttonsContainer = GetNode<HBoxContainer>("VBoxContainer/HBoxContainer");
        confirmButton = GetNode<Button>("VBoxContainer/HBoxContainer/ConfirmButton");
        cancelButton = GetNode<Button>("VBoxContainer/HBoxContainer/CancelButton");
        cancelEndSpacer = GetNode<Control>("VBoxContainer/HBoxContainer/Spacer");
        focusGrabber = GetNode<FocusGrabber>("VBoxContainer/FocusGrabber");

        // Only move the buttons when run outside of the editor to avoid messing up
        // the predefined button order placement in the scene when it's opened
        if (OS.IsOkLeftAndCancelRight() && !Engine.EditorHint)
        {
            buttonsContainer.MoveChild(confirmButton, 1);
            buttonsContainer.MoveChild(cancelButton, 3);
            cancelEndSpacer = GetNode<Control>("VBoxContainer/HBoxContainer/Spacer3");
        }

        UpdateLabel();
        UpdateButtons();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateLabel();
            UpdateButtons();
        }

        base._Notification(what);
    }

    public void SetConfirmDisabled(bool disabled)
    {
        if (confirmButton == null)
            throw new SceneTreeAttachRequired();

        confirmButton.Disabled = disabled;
    }

    private void UpdateLabel()
    {
        if (dialogLabel == null)
            throw new SceneTreeAttachRequired();

        var text = TranslationServer.Translate(dialogText);
        dialogLabel.ExtendedBbcode = CenterText ? $"[center]{text}[/center]" : text;
    }

    private void UpdateButtons()
    {
        if (cancelButton == null || confirmButton == null || focusGrabber == null)
            throw new SceneTreeAttachRequired();

        cancelButton.Visible = !hideCancelButton;
        cancelEndSpacer.Visible = !hideCancelButton;
        focusGrabber.NodeToGiveFocusTo = focusGrabber.GetPathTo(hideCancelButton ? confirmButton : cancelButton);

        confirmButton.Text = TranslationServer.Translate(confirmText);
        cancelButton.Text = TranslationServer.Translate(cancelText);
    }

    private void OnConfirmPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (HideOnOk)
            Close();

        EmitSignal(nameof(Confirmed));
    }
}
