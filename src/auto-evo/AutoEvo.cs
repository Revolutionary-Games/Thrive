namespace AutoEvo;

/// <summary>
///   Main helper class for doing auto-evo runs
/// </summary>
public static class AutoEvo
{
    public static AutoEvoRun CreateRun(GameWorld world, AutoEvoGlobalCache globalCache)
    {
        var result = new AutoEvoRun(world, globalCache);

        if (Settings.Instance.RunAutoEvoDuringGamePlay)
        {
            result.Start();
        }

        return result;
    }
}
