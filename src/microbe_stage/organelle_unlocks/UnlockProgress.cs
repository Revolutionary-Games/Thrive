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
        SimulationParameters.Instance.GetAllOrganelles().Where(organelle => organelle.UnlockConditions == null).ToHashSet();

    /// <summary>
    ///   Unlock an organelle, returning if this is the first time it has been unlocked.
    /// </summary>
    public bool UnlockOrganelle(OrganelleDefinition organelle)
    {
        return unlockedOrganelles.Add(organelle);
    }

    /// <summary>
    ///   Is the organelle unlocked?
    /// </summary>
    public bool IsUnlocked(OrganelleDefinition organelle, GameWorld world)
    {
        if (organelle.UnlockConditions != null)
        {
            if (organelle.UnlockConditions.Any(unlock => unlock.Satisfied(world)))
                UnlockOrganelle(organelle);

            GD.Print(organelle.Name, " unlocked = ", unlockedOrganelles.Contains(organelle));
            foreach (var unlock in organelle.UnlockConditions)
            {
                GD.Print("- ", unlock.Tooltip(world), "\" = ", unlock.Satisfied(world));
            }
        }

        return unlockedOrganelles.Contains(organelle);
    }
}
