/// <summary>
///   Helpers for forwarding events related to achievements
/// </summary>
public static class AchievementEvents
{
    public static void ReportPlayerMicrobeKill()
    {
        AchievementsManager.Instance.OnPlayerMicrobeKill();
    }

    public static void ReportExitEditorWithoutChanges()
    {
        AchievementsManager.Instance.OnExitEditorWithoutChanges();
    }

    public static void ReportPlayerPhotosynthesisGlucoseBalance(float balance)
    {
        AchievementsManager.Instance.OnPlayerPhotosynthesisGlucoseBalance(balance);
    }
}
