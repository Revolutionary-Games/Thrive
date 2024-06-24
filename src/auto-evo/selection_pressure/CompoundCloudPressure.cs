namespace AutoEvo;

using System;

public class CompoundCloudPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("REACH_COMPOUND_CLOUD_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly float totalEnergy;
    private readonly Compound compound;

    public CompoundCloudPressure(Patch patch, float weight, Compound compound) : base(weight,
        [
            new LowerRigidity(),
            new ChangeMembraneType(SimulationParameters.Instance.GetMembrane("single")),
        ])
    {
        if (!compound.IsCloud)
            throw new ArgumentException("Given compound to cloud pressure is not of cloud type");

        this.compound = compound;

        if (patch.Biome.AverageCompounds.TryGetValue(compound, out var compoundData))
        {
            totalEnergy = compoundData.Density * compoundData.Amount * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;
        }
        else
        {
            totalEnergy = 0.0f;
        }
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        return species.BaseSpeed;
    }

    public override float GetEnergy()
    {
        return totalEnergy;
    }

    public override string ToString()
    {
        return $"{Name} ({compound.Name})";
    }
}
