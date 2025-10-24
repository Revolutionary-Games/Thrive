using System;
using System.Collections.Generic;
using Arch.Core;
using Components;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Adds extra digestion enzymes to an organelle
/// </summary>
public class LysosomeComponent : IOrganelleComponent
{
    public bool UsesSyncProcess { get; set; }

    public static void CalculateLysosomeActiveEnzymes(LysosomeUpgrades? lysosomeData, Dictionary<Enzyme, int> result)
    {
        if (lysosomeData == null)
        {
            result[SimulationParameters.Instance.GetEnzyme(Constants.LIPASE_ENZYME)] = 1;
            return;
        }

        var enzyme = lysosomeData.Enzyme;
        result[enzyme] = 1;
    }

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        var configuration = organelle.Upgrades?.CustomUpgradeData;

        // TODO: avoid allocating memory like this for each lysosome component
        // Could most likely refactor the PlacedOrganelle.GetEnzymes to take in the container.AvailableEnzymes
        // dictionary and write updated values to that
        var enzymes = new Dictionary<Enzyme, int>();
        CalculateLysosomeActiveEnzymes(configuration as LysosomeUpgrades, enzymes);

        organelle.OverriddenEnzymes = enzymes;
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity,
        IWorldSimulation worldSimulation, float delta)
    {
        // TODO: Animate lysosomes sticking onto phagosomes (if possible). This probably should happen in the
        // engulfing system (this at least can't happen here as Godot data update needs to happen in sync update)
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        throw new NotSupportedException();
    }
}

public class LysosomeComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new LysosomeComponent();
    }

    public void Check(string name)
    {
    }
}

[JSONDynamicTypeAllowed]
public class LysosomeUpgrades : IComponentSpecificUpgrades
{
    public const ushort SERIALIZATION_VERSION = 1;

    public LysosomeUpgrades(Enzyme enzyme)
    {
        Enzyme = enzyme;
    }

    public Enzyme Enzyme { get; set; }

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.LysosomeUpgrades;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => false;

    public static LysosomeUpgrades ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new LysosomeUpgrades(reader.ReadObject<Enzyme>());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Enzyme);
    }

    public bool Equals(IComponentSpecificUpgrades? other)
    {
        if (other is not LysosomeUpgrades otherLysosome)
            return false;

        return Enzyme.InternalName.Equals(otherLysosome.Enzyme.InternalName);
    }

    public object Clone()
    {
        return new LysosomeUpgrades(Enzyme);
    }

    public override int GetHashCode()
    {
        return int.RotateRight(Enzyme.InternalName.GetHashCode(), 3);
    }

    public ulong GetVisualHashCode()
    {
        // Doesn't impact the visuals at all
        return 3;
    }
}
