using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;

internal class UpgradeOrganelle : IMutationStrategy<MicrobeSpecies>
{
    private readonly FrozenSet<OrganelleDefinition> allOrganelles;
    private IComponentSpecificUpgrades upgrade;

    public UpgradeOrganelle(Func<OrganelleDefinition, bool> criteria, IComponentSpecificUpgrades upgrade)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        this.upgrade = upgrade;
    }

    public bool Repeatable => true;

    public List<Tuple<MicrobeSpecies, float>>? MutationsOf(MicrobeSpecies baseSpecies, float mp, bool lawk,
    Random random)
    {
        var mutated = new List<Tuple<MicrobeSpecies, float>>();

        MicrobeSpecies newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        foreach (OrganelleTemplate organelle in newSpecies.Organelles.Where(x => allOrganelles.Contains(x.Definition)))
        {
            if (organelle.Upgrades == null)
            {
                organelle.Upgrades = new OrganelleUpgrades();
            }

            organelle.Upgrades.CustomUpgradeData = upgrade;
        }

        mutated.Add(new Tuple<MicrobeSpecies, float>(newSpecies, mp));

        return mutated;
    }
}
