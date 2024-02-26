using Godot;

public partial class PermanentlyDismissibleDialog : CustomConfirmationDialog
{
    [Export]
    public DismissibleNotice NoticeType;

    [Export]
    public DialogTypeEnum DialogType;

    [Export]
    public PermanentDismissTypeEnum PermanentDismissType = PermanentDismissTypeEnum.RememberOnConfirm;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private CustomCheckBox checkbox = null!;
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

        checkbox = GetNode<CustomCheckBox>("VBoxContainer/CheckBox");

        switch (DialogType)
        {
            case DialogTypeEnum.Information:
                checkbox.Text = TranslationServer.Translate("DISMISS_INFORMATION_PERMANENTLY");
                break;
            case DialogTypeEnum.Warning:
                checkbox.Text = TranslationServer.Translate("DISMISS_WARNING_PERMANENTLY");
                break;
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
            return true;
        }

        return false;
    }

    private void OnConfirmed()
    {
        if (checkbox.ButtonPressed && PermanentDismissType == PermanentDismissTypeEnum.RememberOnConfirm)
            Settings.Instance.PermanentlyDismissNotice(NoticeType);
    }

    private void OnCancelled()
    {
        if (checkbox.ButtonPressed && PermanentDismissType == PermanentDismissTypeEnum.RememberOnCancel)
            Settings.Instance.PermanentlyDismissNotice(NoticeType);
    }
}
