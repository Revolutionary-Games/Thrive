namespace AutoEvo;

using SharedBase.Archive;

public class EnergyConsumptionPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("ENERGY_CONSUMPTION_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public EnergyConsumptionPressure(float weight) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.HasBindingFeature),
            new AddOrganelleAnywhere(organelle => organelle.HasSignalingFeature),
        ])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.EnergyConsumptionPressure;

    public static EnergyConsumptionPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new EnergyConsumptionPressure(reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);
        var activityScore = species.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;

        // Calculate how much energy is typically being consumed
        var energyConsumption = (1 - activityScore) * energyBalance.TotalConsumptionStationary;
        energyConsumption += activityScore * energyBalance.TotalConsumption;

        // Modifier to fit the current mechanics of the Binding Agent. This should probably be removed or adjusted if
        // being in a colony no longer reduces osmoregulation cost.
        var bindingModifier = 1.0f;

        MicrobeInternalCalculations.GetBindingAndSignalling(microbeSpecies.Organelles.Organelles,
            out var hasBindingAgent, out var hasSignallingAgent);

        if (hasBindingAgent)
        {
            if (hasSignallingAgent)
            {
                bindingModifier *= 1 -
                    Constants.AUTO_EVO_COLONY_OSMOREGULATION_BONUS * Constants.AUTO_EVO_SIGNALLING_BONUS;
            }
            else
            {
                bindingModifier *= 1 - Constants.AUTO_EVO_COLONY_OSMOREGULATION_BONUS;
            }
        }

        var score = 1 / (energyConsumption * bindingModifier);

        return score;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
