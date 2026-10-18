/// <summary>
///   Helpers for forwarding events related to achievements
/// </summary>
public static class AchievementEvents
{
    public static void ReportMulticellularColonySize(int colonySize)
    {
        AchievementsManager.Instance.OnUltimateColony(colonySize);
    }

    public static void ReportMulticellularToxinsPerUpdate(int projectilesInVolley)
    {
        AchievementsManager.Instance.OnUltimateCellularFirepower(projectilesInVolley);
    }

    public static void ReportYouAnimal()
    {
        AchievementsManager.Instance.OnYouAnimal();
    }

    public static void ReportSpecializedWeapons()
    {
        AchievementsManager.Instance.OnSpecializedWeapons();
    }

    public static void ReportSpecializationSpecialist(int specializedCellTypeCount)
    {
        AchievementsManager.Instance.OnSpecializationSpecialist(specializedCellTypeCount);
    }

    public static void ReportJustAFleshWound()
    {
        AchievementsManager.Instance.OnJustAFleshWound();
    }

    public static void ReportInLookOfAHome(int distanceTravelled)
    {
        AchievementsManager.Instance.OnInLookOfAHome(distanceTravelled);
    }

    public static void ReportMicroscopicBirdsAndBees()
    {
        AchievementsManager.Instance.OnMicroscopicBirdsAndBees();
    }

    public static void ReportPerfectlyPacked()
    {
        AchievementsManager.Instance.OnPerfectlyPacked();
    }

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

    public static void ReportPlayerInCellColony()
    {
        AchievementsManager.Instance.OnPlayerInCellColony();
    }

    public static void ReportReturnToMulticellularStageFromEditor()
    {
        AchievementsManager.Instance.OnReturnToMulticellularStageFromEditor();
    }

    public static void ReportReturnToMacroscopicStageFromEditor()
    {
        AchievementsManager.Instance.OnReturnToMacroscopicStageFromEditor();
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
