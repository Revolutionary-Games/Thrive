namespace AutoEvo;

using SharedBase.Archive;

public class CompoundConversionEfficiencyPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    public readonly CompoundDefinition FromCompound;

    public readonly CompoundDefinition ToCompound;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_EFFICIENCY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly bool usedForSurvival;

    public CompoundConversionEfficiencyPressure(Compound compound, Compound outCompound,
        bool usedForSurvival, float weight) :
        base(weight, [
            AddOrganelleAnywhere.ThatConvertBetweenCompounds(compound, outCompound),
            new AddOrganelleAnywhere(organelle => organelle.HasBindingFeature),
            new AddOrganelleAnywhere(organelle => organelle.HasSignalingFeature),
            RemoveOrganelle.ThatCreateCompound(outCompound),
            new ChangeMembraneType("double"),
            new ChangeMembraneType("cellulose"),
            new ChangeMembraneType("chitin"),
            new ChangeMembraneType("calciumCarbonate"),
            new ChangeMembraneType("silica"),
        ])
    {
        FromCompound = SimulationParameters.GetCompound(compound);
        ToCompound = SimulationParameters.GetCompound(outCompound);
        this.usedForSurvival = usedForSurvival;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CompoundConversionEfficiencyPressure;

    public static CompoundConversionEfficiencyPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new CompoundConversionEfficiencyPressure((Compound)reader.ReadInt32(),
            (Compound)reader.ReadInt32(), reader.ReadBool(), reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)FromCompound.ID);
        writer.Write((int)ToCompound.ID);
        writer.Write(usedForSurvival);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var score = cache.GetCompoundConversionScoreForSpecies(FromCompound, ToCompound, microbeSpecies, patch.Biome);

        var activityScore = species.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;

        // Calculate how much energy is typically being consumed
        var energyConsumption = (1 - activityScore) *
            cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome).TotalConsumptionStationary;
        energyConsumption +=
            activityScore * cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome).TotalConsumption;

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

        // we need to factor in both conversion from source to output, and energy expenditure time
        if (usedForSurvival)
        {
            score /= energyConsumption * bindingModifier;
        }

        return score;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }

    public override string ToString()
    {
        return $"{Name} ({FromCompound.Name})";
    }
}
