using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;

public class UpgradeOrganelle : IMutationStrategy<MicrobeSpecies>
{
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;
    private readonly IComponentSpecificUpgrades? customUpgrade;
    private readonly string? upgradeName;
    private readonly bool shouldRepeat;

    /// <summary>
    ///   Updates an organelle type with the given upgrade custom data
    /// </summary>
    /// <param name="criteria">Organelle requirement to apply</param>
    /// <param name="customUpgrade">The custom data to be applied as an upgrade to the organelle</param>
    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, IComponentSpecificUpgrades customUpgrade)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        shouldRepeat = false;
        this.customUpgrade = customUpgrade;
    }

    /// <summary>
    ///   Updates an organelle type with the given upgrade, creating a new species to test for every additional
    ///   organelle upgraded.
    /// </summary>
    /// <param name="criteria">Organelle requirement to apply</param>
    /// <param name="upgradeName">The name of the upgrade (from organelles.json) to apply</param>
    /// <param name="shouldRepeat">
    ///   Determines whether this mutation strategy can be used multiple times
    ///   should be false for any upgrade that does not cost MP
    /// </param>
    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, string upgradeName, bool shouldRepeat)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        foreach (var organelle in allOrganelles)
        {
            if (!organelle.AvailableUpgrades.ContainsKey(upgradeName))
            {
                throw new ArgumentException(organelle.Name + " does not have upgrade: " + upgradeName);
            }
        }

        this.shouldRepeat = shouldRepeat;
        this.upgradeName = upgradeName;
    }

    public bool Repeatable => shouldRepeat;

    public List<Tuple<MicrobeSpecies, double>>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (allOrganelles.Count == 0)
        {
            return null;
        }

        // TODO: maybe there is a way to avoid this memory allocation?
        List<int>? organelleIndexesToMutate = null;

        // Manual looping to avoid one enumerator allocation per call
        var organelleList = baseSpecies.Organelles.Organelles;
        var organelleCount = organelleList.Count;
        for (var i = 0; i < organelleCount; ++i)
        {
            var organelle = organelleList[i];
            if (!allOrganelles.Contains(organelle.Definition))
                continue;

            // Filter out upgrades early that cost too much
            double mpCost = 0;
            bool canUpgrade = false;

            if (upgradeName != null)
            {
                foreach (var availableUpgrade in organelle.Definition.AvailableUpgrades)
                {
                    // Filter to just appliable upgrades
                    if (availableUpgrade.Key == upgradeName && (organelle.Upgrades == null ||
                            !organelle.Upgrades.UnlockedFeatures.Contains(upgradeName)))
                    {
                        mpCost += availableUpgrade.Value.MPCost;
                        canUpgrade = true;
                        break;
                    }
                }
            }

            if (customUpgrade != null)
            {
                mpCost += customUpgrade.CalculateCost(organelle.Upgrades?.CustomUpgradeData);
                canUpgrade = true;
            }

            // Don't add to the mutations-to-try-list if too expensive
            if (mpCost > mp || (customUpgrade == null && !canUpgrade))
                continue;

            organelleIndexesToMutate ??= new List<int>();
            organelleIndexesToMutate.Add(i);
        }

        // Skip if nothing can be upgraded
        if (organelleIndexesToMutate == null)
        {
            return null;
        }

        var mutated = new List<Tuple<MicrobeSpecies, double>>();

        // Pick a random organelle that can be mutated each time
        organelleIndexesToMutate.Shuffle(random);

        for (int i = 0; i < Constants.AUTO_EVO_ORGANELLE_UPGRADE_ATTEMPTS; ++i)
        {
            if (i >= organelleIndexesToMutate.Count)
                break;

            int organelleToMutate = organelleIndexesToMutate[i];

            // We manually clone organelles to save on memory allocation
            var newSpecies = baseSpecies.Clone(false);

            bool mutatedOrganelle = false;
            double mpCost = 0;

            // TODO: this whole block would need to run twice if we wanted to try the feature flag and custom data
            // separately like in one version of this code hack
            for (var j = 0; j < organelleCount; ++j)
            {
                if (j == organelleToMutate)
                {
                    var originalOrganelle = organelleList[j];

                    bool hasFeatureUpgrade = false;

                    // We need to re-calculate the cost here as not all organelles will have a uniform cost based
                    // on what upgrades they have already applied (and we need to know that for the final cost)
                    if (upgradeName != null)
                    {
                        foreach (var availableUpgrade in originalOrganelle.Definition.AvailableUpgrades)
                        {
                            // Check if found an available upgrade that is not applied yet
                            if (availableUpgrade.Key == upgradeName && (originalOrganelle.Upgrades == null ||
                                    !originalOrganelle.Upgrades.UnlockedFeatures.Contains(upgradeName)))
                            {
                                mpCost += availableUpgrade.Value.MPCost;
                                hasFeatureUpgrade = true;
                                break;
                            }
                        }
                    }

                    if (customUpgrade != null)
                    {
                        mpCost += customUpgrade.CalculateCost(originalOrganelle.Upgrades?.CustomUpgradeData);
                    }

                    if (mpCost > mp || (customUpgrade == null && !hasFeatureUpgrade))
                    {
                        // This is wasting an attempt as we do not have enough MPs to upgrade this organelle
                        // As we try only one organelle upgrade per loop, we need to abandon this entire species clone
                        // attempt
                        break;
                    }

                    var upgradedOrganelle = organelleList[j].Clone(false);

                    upgradedOrganelle.Upgrades = new OrganelleUpgrades();

                    if (customUpgrade != null)
                    {
                        upgradedOrganelle.Upgrades.CustomUpgradeData = customUpgrade;
                    }

                    if (upgradeName != null && hasFeatureUpgrade &&
                        !upgradedOrganelle.Upgrades.UnlockedFeatures.Contains(upgradeName))
                    {
                        upgradedOrganelle.Upgrades.UnlockedFeatures.Add(upgradeName);
                    }

                    // We did not change the position at all, so we can safely put down the organelle as upgrades
                    // cannot affect the shape
                    newSpecies.Organelles.AddAutoEvoAttemptOrganelle(upgradedOrganelle);
                    mutatedOrganelle = true;
                }
                else
                {
                    newSpecies.Organelles.AddAutoEvoAttemptOrganelle(organelleList[j]);
                }
            }

            if (mutatedOrganelle)
            {
                mutated.Add(new Tuple<MicrobeSpecies, double>(newSpecies, mp - mpCost));
            }
        }

        return mutated;
    }
}
