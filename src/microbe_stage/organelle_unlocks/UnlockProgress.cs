using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Stores which organelles have been unlocked by the player.
/// </summary>
[UseThriveSerializer]
public class UnlockProgress
{
    /// <summary>
    ///   The organelles that the player can use.
    /// </summary>
    [JsonProperty]
    private readonly HashSet<OrganelleDefinition> unlockedOrganelles =
        SimulationParameters
            .Instance
            .GetAllOrganelles()
            .Where(organelle => organelle.UnlockConditions == null)
            .ToHashSet();

    /// <summary>
    ///   Organelles unlocked singe the last time in the editor.
    /// </summary>
    [JsonProperty]
    private readonly HashSet<OrganelleDefinition> recentlyUnlocked = new();

    /// <summary>
    ///   Unlock an organelle, returning true if this is the first time it has been unlocked.
    /// </summary>
    public bool UnlockOrganelle(OrganelleDefinition organelle, GameProperties game)
    {
        if (organelle.UnlockConditions == null || game.FreeBuild)
            return false;

        var firstTimeUnlocking = unlockedOrganelles.Add(organelle);

        if (firstTimeUnlocking)
            recentlyUnlocked.Add(organelle);

        return firstTimeUnlocking;
    }

    /// <summary>
    ///   Is the organelle unlocked?
    /// </summary>
    public bool IsUnlocked(OrganelleDefinition organelle, GameProperties game)
    {
        if (organelle.UnlockConditions == null || game.FreeBuild)
            return true;

        if (organelle.UnlockConditions.Any(unlock => unlock.Satisfied(game.GameWorld)))
            UnlockOrganelle(organelle, game);

        GD.Print(organelle.Name, " unlocked = ", unlockedOrganelles.Contains(organelle));
        foreach (var unlock in organelle.UnlockConditions)
        {
            GD.Print("- ", unlock.Tooltip(game.GameWorld), "\" = ", unlock.Satisfied(game.GameWorld));
        }

        return unlockedOrganelles.Contains(organelle);
    }

    /// <summary>
    ///   Has the organelle been recently unlocked?
    /// </summary>
    public bool RecentlyUnlocked(OrganelleDefinition organelle)
    {
        return recentlyUnlocked.Contains(organelle);
    }

    /// <summary>
    ///   Clear recently unlocked organelles
    /// </summary>
    public void ClearRecentlyUnlocked()
    {
        recentlyUnlocked.Clear();
    }
}
