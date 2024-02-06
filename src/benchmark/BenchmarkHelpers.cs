using Godot;

public static class BenchmarkHelpers
{
    public static void PerformBenchmarkSetup(BenchmarkChangedSettingsStore settingsStore)
    {
        UncapFramerate();
        EnsureNoCheatsEnabled();
        SetConsistentCloudUpdateRate(out settingsStore.CloudSettings);
    }

    public static void RestoreNormalSettings(BenchmarkChangedSettingsStore settingsStore)
    {
        UndoFramerateUncap();
        RestoreCloudUpdateRate(settingsStore.CloudSettings);
    }

    public static string GetGeneralHardwareInfo()
    {
        return $"CPU: {OS.GetProcessorName()} (used tasks: {TaskExecutor.Instance.ParallelTasks}, " +
            $"native: {TaskExecutor.Instance.NativeTasks}, sim threads: " +
            $"{Settings.Instance.RunGameSimulationMultithreaded.Value})\n" +
            $"GPU: {VisualServer.GetVideoAdapterName()}\nOS: {OS.GetName()}";
    }

    /// <summary>
    ///   Uncaps the engine framerate for better performance measuring
    /// </summary>
    private static void UncapFramerate()
    {
        // This needs to be invoked to make sure directly starting benchmark scenes works
        Invoke.Instance.Queue(() =>
        {
            Engine.TargetFps = -1;
            OS.VsyncEnabled = false;
        });
    }

    private static void UndoFramerateUncap()
    {
        // Reapply the settings we overrode
        Settings.Instance.ApplyGraphicsSettings();
        Settings.Instance.ApplyWindowSettings();
    }

    private static void EnsureNoCheatsEnabled()
    {
        CheatManager.DisableAllCheats();
    }

    private static void SetConsistentCloudUpdateRate(out float settingStore)
    {
        settingStore = Settings.Instance.CloudUpdateInterval;
        Settings.Instance.CloudUpdateInterval.Value = 0.02f;
    }

    private static void RestoreCloudUpdateRate(float settingStore)
    {
        Settings.Instance.CloudUpdateInterval.Value = settingStore;
    }

    public class BenchmarkChangedSettingsStore
    {
        public float CloudSettings;
    }
}
