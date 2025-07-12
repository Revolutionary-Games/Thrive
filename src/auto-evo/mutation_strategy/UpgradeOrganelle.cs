using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using static CommonMutationFunctions;

public class UpgradeOrganelle : IMutationStrategy<MicrobeSpecies>
{
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;
    private readonly IComponentSpecificUpgrades? customUpgrade;
    private readonly string? upgradeName;

    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, IComponentSpecificUpgrades customUpgrade)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        this.customUpgrade = customUpgrade;
    }

    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, string upgradeName)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        foreach (var organelle in allOrganelles)
        {
            if (!organelle.AvailableUpgrades.ContainsKey(upgradeName))
            {
                throw new ArgumentException(organelle.Name + " does not have upgrade: " + upgradeName);
            }
        }

        this.upgradeName = upgradeName;
    }

    public bool Repeatable => false;

    public List<Mutant>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
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

        if (validMutations)
        {
            var mutated = new List<Mutant>();

            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            organelleList = newSpecies.Organelles.Organelles;
            count = organelleList.Count;
            for (var i = 0; i < count; ++i)
            {
                var organelle = organelleList[i];

                if (allOrganelles.Contains(organelle.Definition))
                {
                    // TODO: Once this is used with an upgrade that costs MP this will need to factor that in
                    organelle.Upgrades ??= new OrganelleUpgrades();
                    if (customUpgrade != null)
                    {
                        organelle.Upgrades.CustomUpgradeData = customUpgrade;
                    }

                    if (upgradeName != null && !organelle.Upgrades.UnlockedFeatures.Contains(upgradeName))
                    {
                        organelle.Upgrades.UnlockedFeatures.Add(upgradeName);
                    }
                }
            }

            mutated.Add(new Mutant(newSpecies, mp));

            return mutated;
        }

        return null;
    }
}
