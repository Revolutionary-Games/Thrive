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

    private void PopupClosed()
    {
        EmitSignal(SignalName.OnClosed);
    }

    private void BackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(SignalName.OnClosed);
        achievementsPopup.Close();
    }
}
