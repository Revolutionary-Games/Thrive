using Godot;

/// <summary>
///   Provides a popup container for the achievements gallery
/// </summary>
public partial class AchievementsView : Control
{
#pragma warning disable CA2213
    [Export]
    private CustomWindow achievementsPopup = null!;

    [Export]
    private AchievementsGallery achievementsGallery = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnClosedEventHandler();

    /// <summary>
    ///   Lets the containing menu close the popup after animating a close request.
    /// </summary>
    public bool DeferPopupClose
    {
        get => !achievementsPopup.CloseOnTitleBarButtonPress;
        set => achievementsPopup.CloseOnTitleBarButtonPress = !value;
    }

    /// <summary>
    ///   Opens a popup for achievements and refreshes them
    /// </summary>
    public void OpenPopup()
    {
        achievementsPopup.OpenCentered(false);

        achievementsGallery.Refresh();

        // For fun show how many achievements are unlocked
        int total = 0;
        int unlocked = 0;

        foreach (var achievement in AchievementsManager.Instance.GetAchievements())
        {
            ++total;

            if (achievement.Achieved)
                ++unlocked;
        }

        achievementsPopup.WindowTitle = Localization.Translate("ACHIEVEMENTS_TOTAL").FormatSafe(unlocked, total);
    }

    public void SetPopupOpacity(float opacity)
    {
        achievementsPopup.Modulate = achievementsPopup.Modulate with { A = opacity };
    }

    public void ClosePopup()
    {
        achievementsPopup.Close();
    }

    private void PopupCloseRequested()
    {
        EmitSignal(SignalName.OnClosed);
    }

    private void PopupClosed()
    {
        if (!DeferPopupClose)
            EmitSignal(SignalName.OnClosed);
    }

    private void BackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        PopupCloseRequested();

        if (!DeferPopupClose)
            ClosePopup();
    }
}
