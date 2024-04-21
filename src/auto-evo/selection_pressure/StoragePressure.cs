namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

public class StoragePressure : SelectionPressure
{
    public Compound Compound;
    private readonly float weight;
    public StoragePressure(float weight, Compound compound) : base(
        weight,
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            new AddOrganelleAnywhere(organelle => organelle.Components?.Storage?.Capacity > 0.5f),
        })
    {
        Compound = compound;
        this.weight = weight;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        return species.StorageCapacity * weight;
    }
}
