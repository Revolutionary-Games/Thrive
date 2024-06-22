namespace AutoEvo;

public class StoragePressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("STORAGE_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    public readonly Compound Compound;
    private readonly float weight;

    public StoragePressure(float weight, Compound compound) : base(weight,
        [
            new AddOrganelleAnywhere(organelle => organelle.Components.Storage?.Capacity > 0.5f),
        ],
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
