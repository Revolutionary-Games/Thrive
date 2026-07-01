using Godot;

// Commands related to cache management for developers
public static class CacheCommands
{
    [Command("clear_membrane_cache", true, "Clears the cached membrane point data.")]
    private static void ClearMembraneCache(CommandContext context)
    {
        ProceduralDataCache.Instance.ClearMembraneCache();
        context.Print("Cleared membrane cache.");
    }
}
