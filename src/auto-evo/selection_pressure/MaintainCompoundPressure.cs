namespace AutoEvo;

using System;
using SharedBase.Archive;

public class MaintainCompoundPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_MAINTAIN_COMPOUND_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly CompoundDefinition compound;

    public MaintainCompoundPressure(Compound compound, float weight) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound(compound),
        RemoveOrganelle.ThatUseCompound(compound),
    ])
    {
        this.compound = SimulationParameters.GetCompound(compound);
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MaintainCompoundPressure;

    public static MaintainCompoundPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MaintainCompoundPressure((Compound)reader.ReadInt32(), reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)compound.ID);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var compoundUsed = 0.0f;
        var compoundCreated = 0.0f;

        var biomeConditions = patch.Biome;
        var resolvedTolerances = cache.GetEnvironmentalTolerances(microbeSpecies, biomeConditions);

        for (var i = 0; i < microbeSpecies.Organelles.Count; ++i)
        {
            var organelle = microbeSpecies.Organelles[i];
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
                {
                    var processSpeed = cache
                        .GetProcessMaximumSpeed(process, resolvedTolerances.ProcessSpeedModifier, biomeConditions)
                        .CurrentSpeed;

                    compoundUsed += inputAmount * processSpeed;
                }

                if (process.Process.Outputs.TryGetValue(compound, out var outputAmount))
                {
                    var processSpeed = cache
                        .GetProcessMaximumSpeed(process, resolvedTolerances.ProcessSpeedModifier, biomeConditions)
                        .CurrentSpeed;

                    compoundCreated += outputAmount * processSpeed;
                }
            }
        }

        if (compoundCreated <= 0 || compoundUsed <= 0)
            return 0.0f;

        return MathF.Min(compoundCreated / compoundUsed, 1);
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
