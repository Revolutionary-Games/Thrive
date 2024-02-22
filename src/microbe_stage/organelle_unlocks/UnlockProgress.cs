using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnlockConstraints;

/// <summary>
///   Stores which organelles have been unlocked by the player.
/// </summary>
[UseThriveSerializer]
public class UnlockProgress
{
    /// <summary>
    ///   The organelles that the player can use.
    ///   The default value is the by default unlocked organelles (ones without a condition)
    /// </summary>
    [JsonProperty]
    private readonly HashSet<OrganelleDefinition> unlockedOrganelles =
        SimulationParameters.Instance.GetAllOrganelles().Where(o => o.UnlockConditions == null)
            .ToHashSet();

    /// <summary>
    ///   Organelles unlocked singe the last time in the editor.
    /// </summary>
    [JsonProperty]
    private readonly HashSet<OrganelleDefinition> recentlyUnlocked = new();

    /// <summary>
    ///   If true, <see cref="IsUnlocked"/> will always return true
    /// </summary>
    [JsonProperty]
    public bool UnlockAll { get; set; }

    public static bool SupportsGameState(MainGameState state)
    {
        return state == MainGameState.MicrobeStage;
    }

    /// <summary>
    ///   Unlock an organelle, returning true if this is the first time it has been unlocked.
    /// </summary>
    public bool UnlockOrganelle(OrganelleDefinition organelle, GameProperties game)
    {
        if (organelle.UnlockConditions == null || game.FreeBuild || UnlockAll)
            return false;

        var firstTimeUnlocking = unlockedOrganelles.Add(organelle);

        if (firstTimeUnlocking)
            recentlyUnlocked.Add(organelle);

        return firstTimeUnlocking;
    }

    /// <summary>
    ///   Is the organelle unlocked?
    /// </summary>
    public bool IsUnlocked(OrganelleDefinition organelle, WorldAndPlayerDataSource worldAndPlayerArgs,
        GameProperties game, bool autoUnlock)
    {
        if (organelle.UnlockConditions == null || game.FreeBuild || UnlockAll)
            return true;

        if (autoUnlock)
        {
            bool anyConditionSatisfied = false;
            foreach (var condition in organelle.UnlockConditions)
            {
                if (condition.Satisfied(worldAndPlayerArgs))
                {
                    anyConditionSatisfied = true;
                    break;
                }
            }

            if (anyConditionSatisfied)
            {
                UnlockOrganelle(organelle, game);
                return true;
            }
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
    ///   Clear recently unlocked organelles.
    /// </summary>
    public void ClearRecentlyUnlocked()
    {
        recentlyUnlocked.Clear();
    }
}
