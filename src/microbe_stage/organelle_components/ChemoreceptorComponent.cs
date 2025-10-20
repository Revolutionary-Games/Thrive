using System;
using System.Collections.Generic;
using Arch.Core;
using Components;
using Godot;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Adds radar capability to a cell
/// </summary>
public class ChemoreceptorComponent : IOrganelleComponent
{
    // Either target compound or species should be null (or invalid)
    private Compound targetCompound;
    private Species? targetSpecies;
    private float searchRange;
    private float searchAmount;
    private Color lineColour = Colors.White;

    public bool UsesSyncProcess => false;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        var configuration = organelle.Upgrades?.CustomUpgradeData;

        // Use default values if not configured
        if (configuration == null)
        {
            SetDefaultConfiguration();
            return;
        }

        SetConfiguration((ChemoreceptorUpgrades)configuration);

        if (targetCompound == Compound.Invalid && targetSpecies == null)
            GD.PrintErr("Chemoreceptor has no target compound or species, invalid configuration");
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity,
        IWorldSimulation worldSimulation, float delta)
    {
        if (targetCompound != Compound.Invalid)
        {
            organelleContainer.ActiveCompoundDetections ??=
                new HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)>();

            organelleContainer.ActiveCompoundDetections.Add((targetCompound, searchRange, searchAmount, lineColour));
        }
        else if (targetSpecies != null)
        {
            organelleContainer.ActiveSpeciesDetections ??=
                new HashSet<(Species TargetSpecies, float Range, Color Colour)>();

            organelleContainer.ActiveSpeciesDetections.Add((targetSpecies, searchRange, lineColour));
        }
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        throw new NotSupportedException();
    }

    private void SetConfiguration(ChemoreceptorUpgrades configuration)
    {
        targetCompound = configuration.TargetCompound;
        targetSpecies = configuration.TargetSpecies;
        searchRange = configuration.SearchRange;
        searchAmount = configuration.SearchAmount;
        lineColour = configuration.LineColour;
    }

    private void SetDefaultConfiguration()
    {
        targetCompound = Constants.CHEMORECEPTOR_DEFAULT_COMPOUND;
        targetSpecies = null;
        searchRange = Constants.CHEMORECEPTOR_RANGE_DEFAULT;
        searchAmount = Constants.CHEMORECEPTOR_AMOUNT_DEFAULT;
        lineColour = Colors.White;
    }
}

public class ChemoreceptorComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new ChemoreceptorComponent();
    }

    public void Check(string name)
    {
    }
}

[JSONDynamicTypeAllowed]
public class ChemoreceptorUpgrades : IComponentSpecificUpgrades
{
    public const ushort SERIALIZATION_VERSION = 1;

    public ChemoreceptorUpgrades(Compound targetCompound, Species? targetSpecies,
        float searchRange, float searchAmount, Color lineColour)
    {
        TargetCompound = targetCompound;
        TargetSpecies = targetSpecies;
        SearchRange = searchRange;
        SearchAmount = searchAmount;
        LineColour = lineColour;
    }

    public Compound TargetCompound { get; set; }
    public Species? TargetSpecies { get; set; }
    public float SearchRange { get; set; }
    public float SearchAmount { get; set; }
    public Color LineColour { get; set; }

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ChemoreceptorUpgrades;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => false;

    public static ChemoreceptorUpgrades ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new ChemoreceptorUpgrades((Compound)reader.ReadInt32(), reader.ReadObjectOrNull<Species>(),
            reader.ReadFloat(), reader.ReadFloat(), reader.ReadColor());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)TargetCompound);
        writer.WriteObjectOrNull(TargetSpecies);
        writer.Write(SearchRange);
        writer.Write(SearchAmount);
        writer.Write(LineColour);
    }

    public bool Equals(IComponentSpecificUpgrades? other)
    {
        if (other is not ChemoreceptorUpgrades otherChemoreceptor)
            return false;

        return TargetCompound == otherChemoreceptor.TargetCompound
            && TargetSpecies?.ID == otherChemoreceptor.TargetSpecies?.ID
            && SearchRange == otherChemoreceptor.SearchRange
            && SearchAmount == otherChemoreceptor.SearchAmount
            && LineColour == otherChemoreceptor.LineColour;
    }

    public object Clone()
    {
        return new ChemoreceptorUpgrades(TargetCompound, TargetSpecies, SearchRange, SearchAmount, LineColour);
    }

    public override int GetHashCode()
    {
        return 283 * TargetCompound.GetHashCode() ^ 293 * TargetSpecies?.GetHashCode() ?? 2579 ^
            307 * SearchRange.GetHashCode() ^ 311 * SearchAmount.GetHashCode() ^ 313 * LineColour.GetHashCode();
    }

    public ulong GetVisualHashCode()
    {
        // This upgrade doesn't impact the visuals at all
        return 2579;
    }
}
