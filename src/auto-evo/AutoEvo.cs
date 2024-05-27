namespace AutoEvo;

/// <summary>
///   Main helper class for doing auto-evo runs
/// </summary>
public static class AutoEvo
{
    public static AutoEvoRun CreateRun(GameWorld world)
    {
        var result = new AutoEvoRun(world);

        if (Settings.Instance.RunAutoEvoDuringGamePlay)
        {
            result.Start();
        }

        return result;
    }
}
