using Godot;

public class PermanentlyDismissableDialog : CustomConfirmationDialog
{
    [Export]
    public DismissibleNotice NoticeType;

#pragma warning disable CA2213 // Disposable fields should be disposed
    private CustomCheckBox checkbox = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    public override void _Ready()
    {
        base._Ready();

        checkbox = GetNode<CustomCheckBox>("VBoxContainer/CheckBox");
    }

    /// <summary>
    ///   Shows this notice by first checking whether the <see cref="NoticeType"/> for this has been
    ///   permanently dismissed and if so cancels popup.
    /// </summary>
    /// <returns><see langword="true"/> if hasn't been permanently dismissed yet.</returns>
    public bool Popup()
    {
        if (!Settings.Instance.IsNoticePermanentlyDismissed(NoticeType))
        {
            this.PopupCenteredShrink();
            return true;
        }

        return false;
    }

    private void OnHide()
    {
        if (checkbox.Pressed)
            Settings.Instance.PermanentlyDismissNotice(NoticeType);
    }
}
