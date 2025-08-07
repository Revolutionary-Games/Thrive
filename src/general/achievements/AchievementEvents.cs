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

    public static void ReportPlayerSurvivedWithNucleus()
    {
        AchievementsManager.Instance.OnPlayerSurvivedWithNucleus();
    }

    public static void ReportEndosymbiosisCompleted()
    {
        AchievementsManager.Instance.OnEndosymbiosisCompleted();
    }

    public static void ReportPlayerDidNotEditSpecies()
    {
        AchievementsManager.Instance.OnPlayerDidNotEditSpecies();
    }

    public static void ReportPlayerInCellColony()
    {
        AchievementsManager.Instance.OnPlayerInCellColony();
    }

    public static void ReportReturnToMulticellularStageFromEditor()
    {
        AchievementsManager.Instance.OnReturnToMulticellularStageFromEditor();
    }

    public static void ReportReturnToMicrobeStageFromEditor()
    {
        AchievementsManager.Instance.OnReturnToMicrobeStageFromEditor();
    }

    public static void ReportHighestPlayerGeneration(int playerSpeciesGeneration)
    {
        AchievementsManager.Instance.OnHighestPlayerGeneration(playerSpeciesGeneration);
    }

    public static void ReportPlayerDigestedObject()
    {
        AchievementsManager.Instance.OnPlayerDigestedObject();
    }

    public static void ReportPlayerUsesRadiation()
    {
        AchievementsManager.Instance.OnPlayerUsesRadiation();
    }

    public static void ReportPlayerUsesChemosynthesis()
    {
        AchievementsManager.Instance.OnPlayerUsesChemosynthesis();
    }
}
