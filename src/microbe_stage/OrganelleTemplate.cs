using System;
using System.Collections.Generic;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Basically just adding the positioning info on top of OrganelleDefinition.
///   When the layout is instantiated in a cell, the PlacedOrganelle class is used.
/// </summary>
public class OrganelleTemplate : IReadOnlyOrganelleTemplate, IPositionedOrganelle, IActionHex, IPlayerReadableName,
    ICloneable, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 2;

    private bool isEndosymbiont;

    public OrganelleTemplate(OrganelleDefinition definition, Hex location, int rotation, bool isEndosymbiont = false)
    {
        Definition = definition;
        Position = location;
        Orientation = rotation;
        this.isEndosymbiont = isEndosymbiont;
    }

    /// <summary>
    ///   The type of the organelle. This is a protected settable for very specific uses of cached objects, but in
    ///   general the type should never change.
    /// </summary>
    public OrganelleDefinition Definition { get; protected set; }

    public Hex Position { get; set; }

    public Vector3 OrganelleModelPosition => Hex.AxialToCartesian(Position) + Definition.ModelOffset;

    /// <summary>
    ///   This is now the number of times to rotate. This used to be the angle in degrees
    /// </summary>
    public int Orientation { get; set; }

    /// <summary>
    ///   The upgrades this organelle will have when instantiated in a microbe
    /// </summary>
    public virtual OrganelleUpgrades? ModifiableUpgrades { get; set; }

    public virtual IReadOnlyOrganelleUpgrades? Upgrades => ModifiableUpgrades;

    public virtual bool IsEndosymbiont { get => isEndosymbiont; set => isEndosymbiont = value; }

    public string ReadableName => Definition.Name;

    public string ReadableExactIdentifier => Localization.Translate("ITEM_AT_2D_COORDINATES")
        .FormatSafe(Definition.Name, Position.Q, Position.R);

    public IReadOnlyList<Hex> RotatedHexes => Definition.GetRotatedHexes(Orientation);

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.OrganelleTemplate;

    public bool CanBeReferencedInArchive => true;

#pragma warning disable CA1033
    OrganelleDefinition IReadOnlyPositionedOrganelle.Definition => Definition;
#pragma warning restore CA1033

    public static OrganelleTemplate ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new OrganelleTemplate(reader.ReadObject<OrganelleDefinition>(), reader.ReadHex(), reader.ReadInt32())
        {
            ModifiableUpgrades = reader.ReadObjectOrNull<OrganelleUpgrades>(),
            IsEndosymbiont = version > 1 && reader.ReadBool(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Definition);
        writer.Write(Position);
        writer.Write(Orientation);
        writer.WriteObjectOrNull(ModifiableUpgrades);
        writer.Write(IsEndosymbiont);
    }

    public bool MatchesDefinition(IActionHex other)
    {
        return Definition == ((OrganelleTemplate)other).Definition;
    }

    /// <summary>
    ///   Calculates the actual active enzymes for this organelle.
    /// </summary>
    /// <param name="result">Puts the results here. Note that this doesn't clear any existing data.</param>
    /// <returns>True if organelle has enzymes (and thus the result was modified)</returns>
    public bool GetActiveEnzymes(Dictionary<Enzyme, int> result)
    {
        if (Definition.HasLysosomeComponent)
        {
            LysosomeComponent.CalculateLysosomeActiveEnzymes(Upgrades?.CustomUpgradeData as LysosomeUpgrades, result);
            return true;
        }

        // No other organelles are known to set up their active enzymes
        return false;
    }

    public Enzyme? GetActiveTargetEnzyme(string internalName)
    {
        if (Definition.HasLysosomeComponent)
        {
            return LysosomeComponent.HasActiveEnzyme(Upgrades?.CustomUpgradeData as LysosomeUpgrades, internalName);
        }

        // No other organelles are known to set up their active enzymes
        return null;
    }

    public float GetActiveToxicity()
    {
        if (Upgrades?.CustomUpgradeData is ToxinUpgrades toxinData)
        {
            return toxinData.Toxicity;
        }

        return Constants.DEFAULT_TOXICITY;
    }

    public ToxinType GetActiveToxin()
    {
        if (Upgrades?.CustomUpgradeData is ToxinUpgrades toxinData)
        {
            return toxinData.BaseType;
        }

        return ToxinType.Cytotoxin;
    }

    public Species? GetActiveTargetSpecies()
    {
        if (Definition.HasChemoreceptorComponent &&
            Upgrades?.CustomUpgradeData is ChemoreceptorUpgrades chemoreceptorData)
        {
            return chemoreceptorData.TargetSpecies;
        }

        // No other organelles are known to set up their active target species
        return null;
    }

    public Compound GetActiveTargetCompound()
    {
        if (Definition.HasChemoreceptorComponent)
        {
            if (Upgrades?.CustomUpgradeData is not ChemoreceptorUpgrades chemoreceptorData)
            {
                return Constants.CHEMORECEPTOR_DEFAULT_COMPOUND;
            }

            return chemoreceptorData.TargetCompound;
        }

        // No other organelles are known to set up their active target compounds
        return Compound.Invalid;
    }

    public OrganelleTemplate Clone()
    {
        return Clone(true);
    }

    public OrganelleTemplate Clone(bool deepClone)
    {
        return new OrganelleTemplate(Definition, Position, Orientation)
        {
            ModifiableUpgrades = deepClone ? ModifiableUpgrades?.Clone() : ModifiableUpgrades,
            IsEndosymbiont = IsEndosymbiont,
        };
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode() * 131 ^ Orientation * 2909 ^ Definition.GetHashCode() * 947 ^
            (Upgrades != null ? Upgrades.GetHashCode() : 1) * 1063;
    }

    public ulong GetVisualHashCode()
    {
        return (ulong)Position.GetHashCode() * 131 ^ (ulong)Orientation * 2909 ^ Definition.GetVisualHashCode() * 947 ^
            (ModifiableUpgrades?.GetVisualHashCode() ?? 1) * 1063;
    }

    public override string ToString()
    {
        return ReadableExactIdentifier;
    }

    object ICloneable.Clone()
    {
        return Clone();
    }
}
