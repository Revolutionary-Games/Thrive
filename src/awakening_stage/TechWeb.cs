using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Main class of the technology "tree" holding player's unlocked technologies and partial progress
/// </summary>
public class TechWeb : IAvailableRecipes
{
    [JsonProperty]
    private readonly HashSet<Technology> unlockedTechnologies = new();

    public delegate void OnTechnologyUnlocked(Technology technology);

    /// <summary>
    ///   Triggered when a new technology is unlocked
    /// </summary>
    [JsonIgnore]
    public OnTechnologyUnlocked? OnTechnologyUnlockedHandler { get; set; }

    /// <summary>
    ///   Unlocks a new technology immediately
    /// </summary>
    /// <param name="technology">The technology to unlock</param>
    /// <returns>True when the technology is now unlocked, false if already unlocked</returns>
    public bool UnlockTechnology(Technology technology)
    {
        if (unlockedTechnologies.Add(technology))
        {
            OnTechnologyUnlockedHandler?.Invoke(technology);
            return true;
        }

        return false;
    }

    public bool HasTechnology(Technology technology)
    {
        return unlockedTechnologies.Contains(technology);
    }

    public IEnumerable<CraftingRecipe> GetAvailableRecipes(
        IReadOnlyCollection<(WorldResource Resource, int Count)>? filter)
    {
        foreach (var technology in unlockedTechnologies)
        {
            foreach (var recipe in technology.GrantsRecipes)
            {
                if (filter != null)
                {
                    if (!recipe.MatchesFilter(filter))
                        continue;
                }

                yield return recipe;
            }
        }
    }

    public IEnumerable<StructureDefinition> GetAvailableStructures()
    {
        return unlockedTechnologies.SelectMany(t => t.GrantsStructures);
    }
}
