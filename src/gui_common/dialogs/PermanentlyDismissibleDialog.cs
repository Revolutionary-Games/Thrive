using Godot;

/// <summary>
///   A dialog that has a checkbox for the user to select to never get this type of popup again
/// </summary>
public partial class PermanentlyDismissibleDialog : CustomConfirmationDialog
{
    [Export]
    public DismissibleNotice NoticeType;

    [Export]
    public DialogTypeEnum DialogType;

    [Export]
    public PermanentDismissTypeEnum PermanentDismissType = PermanentDismissTypeEnum.RememberOnConfirm;

    /// <summary>
    ///   If set to true, then the checkbox to permanently dismiss this dialog is automatically checked when this is
    ///   opened
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that this only works perfectly when using <see cref="PopupIfNotDismissed"/> otherwise this is only
    ///     applied when this enters the scene tree. So if a popup is reused in a way that
    ///     <see cref="PopupIfNotDismissed"/> is not called, this option may not get applied.
    ///   </para>
    /// </remarks>
    [Export]
    public bool AutomaticallyCheckDismissPermanently;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private CheckBox checkbox = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    public enum DialogTypeEnum
    {
        Information,
        Warning,
    }

    public enum PermanentDismissTypeEnum
    {
        RememberOnConfirm,
        RememberOnCancel,
    }

    public override void _Ready()
    {
        base._Ready();

        checkbox = GetNode<CheckBox>("VBoxContainer/CheckBox");

        switch (DialogType)
        {
            case DialogTypeEnum.Information:
                checkbox.Text = Localization.Translate("DISMISS_INFORMATION_PERMANENTLY");
                break;
            case DialogTypeEnum.Warning:
                checkbox.Text = Localization.Translate("DISMISS_WARNING_PERMANENTLY");
                break;
        }

        if (AutomaticallyCheckDismissPermanently)
        {
            checkbox.ButtonPressed = true;
        }
    }

    /// <summary>
    ///   Shows this notice by first checking whether the <see cref="NoticeType"/> for this has been
    ///   permanently dismissed and if so cancels popup.
    /// </summary>
    /// <returns><see langword="true"/> if hasn't been permanently dismissed yet.</returns>
    public bool PopupIfNotDismissed()
    {
        if (!Settings.Instance.IsNoticePermanentlyDismissed(NoticeType))
        {
            PopupCenteredShrink();

            if (AutomaticallyCheckDismissPermanently)
            {
                checkbox.ButtonPressed = true;
            }

            return true;
        }

        return false;
    }

    private void OnConfirmed()
    {
        if (checkbox.ButtonPressed && PermanentDismissType == PermanentDismissTypeEnum.RememberOnConfirm)
            Settings.Instance.PermanentlyDismissNotice(NoticeType);
    }

    private void OnCanceled()
    {
        if (checkbox.ButtonPressed && PermanentDismissType == PermanentDismissTypeEnum.RememberOnCancel)
            Settings.Instance.PermanentlyDismissNotice(NoticeType);
    }
}
