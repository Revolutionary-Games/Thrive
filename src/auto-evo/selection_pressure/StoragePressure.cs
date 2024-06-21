namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;

public class StoragePressure : SelectionPressure
{
    public static readonly LocalizedString Name = new LocalizedString("STORAGE_PRESSURE");
    public readonly Compound Compound;
    private readonly float weight;
    public StoragePressure(float weight, Compound compound) : base(
        weight,
        new List<IMutationStrategy<MicrobeSpecies>>
        {
            new AddOrganelleAnywhere(organelle => organelle.Components?.Storage?.Capacity > 0.5f),
        },
        0)
    {
        Compound = compound;
        this.weight = weight;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        return species.StorageCapacity * weight;
    }

    public override string ToString()
    {
        return $"{Name} ({Compound.Name})";
    }
}
