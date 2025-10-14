using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;

/// <summary>
/// Specific mutationstrategy for toxin-launching organelles
/// applies a fixed toxin type and adjusts toxicity upward, downward, or in both directions
/// </summary>
public class UpgradeToxinOrganelle : IMutationStrategy<MicrobeSpecies>
{
    private readonly Func<OrganelleDefinition, bool> criteria;
    private readonly ToxinType toxinType;
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;
    private readonly string direction;
    private string? upgradeName;

    /// <summary>
    /// Updates a toxin-launching organelle with a given toxin type,
    /// and randomly moves toxicity in a given direction.
    /// </summary>
    /// <param name="criteria">Organelle requirement to apply</param>
    /// <param name="toxinType">The toxin type to assign</param>
    /// <param name="direction"> "increase", "decrease", or "both". To decide what to do with toxicity
    /// </param>
    public UpgradeToxinOrganelle(Func<OrganelleDefinition, bool> criteria, ToxinType toxinType, string direction)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        this.criteria = criteria;
        this.toxinType = toxinType;
        this.direction = direction.ToLowerInvariant();
    }

    // We don't want this to repeat, since changing toxicity does not cost MP
    public bool Repeatable => false;

    public List<Tuple<MicrobeSpecies, double>>? MutationsOf(
        MicrobeSpecies baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (allOrganelles.Count == 0)
        {
            return null;
        }

        bool validMutations = false;

        // Manual looping to avoid one enumerator allocation per call
        var organelleList = baseSpecies.Organelles.Organelles;
        var count = organelleList.Count;
        for (var i = 0; i < count; ++i)
        {
            var organelle = organelleList[i];

            if (allOrganelles.Contains(organelle.Definition))
            {
                validMutations = true;
                break;
            }
        }

        if (!validMutations)
            return null;

        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();
        organelleList = newSpecies.Organelles.Organelles;

        for (var i = 0; i < count; ++i)
        {
            var organelle = organelleList[i];
            if (!criteria(organelle.Definition))
                continue;

            organelle.Upgrades ??= new OrganelleUpgrades();

            // Ensure there is a ToxinUpgrades entry
            if (organelle.Upgrades.CustomUpgradeData is not ToxinUpgrades toxinData)
            {
                toxinData = new ToxinUpgrades(toxinType, 0.0f);
                organelle.Upgrades.CustomUpgradeData = toxinData;
            }
            else
            {
                toxinData.BaseType = toxinType;
            }

            // Make organelle Upgrade match the toxin type
            switch (toxinType)
            {
                case ToxinType.Oxytoxy:
                    upgradeName = "oxytoxy";
                    break;
                case ToxinType.Cytotoxin:
                    upgradeName = "none";
                    break;
                case ToxinType.Macrolide:
                    upgradeName = "macrolide";
                    break;
                case ToxinType.ChannelInhibitor:
                    upgradeName = "channel";
                    break;
                case ToxinType.OxygenMetabolismInhibitor:
                    upgradeName = "oxygen_inhibitor";
                    break;
            }

            organelle.Upgrades.UnlockedFeatures.Add(upgradeName!);

            // Adjust toxicity
            var change = (float)(random.NextDouble() * Constants.AUTO_EVO_MUTATION_TOXICITY_STEP);

            switch (direction)
            {
                case "increase":
                    toxinData.Toxicity = Math.Clamp(toxinData.Toxicity + change, -1.0f, 1.0f);
                    break;

                case "decrease":
                    toxinData.Toxicity = Math.Clamp(toxinData.Toxicity - change, -1.0f, 1.0f);
                    break;

                case "both":
                    change *= random.NextDouble() < 0.5 ? -1f : 1f;
                    toxinData.Toxicity = Math.Clamp(toxinData.Toxicity + change, -1.0f, 1.0f);
                    break;
            }
        }

        return [Tuple.Create(newSpecies, mp)];
    }
}
