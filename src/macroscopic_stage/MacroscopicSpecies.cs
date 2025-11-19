using System;
using System.Collections.Generic;
using System.Linq;
using SharedBase.Archive;

/// <summary>
///   Represents a macroscopic species that is 3D and composed of placed tissues
/// </summary>
public class MacroscopicSpecies : Species, IReadOnlyMacroscopicSpecies
{
    public const ushort SERIALIZATION_VERSION = 1;

    public MacroscopicSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
    {
    }

    public MacroscopicMetaballLayout ModifiableBodyLayout { get; private set; } = new();

    public IReadOnlyMacroscopicMetaballLayout BodyLayout => ModifiableBodyLayout;

    public List<CellType> ModifiableCellTypes { get; private set; } = new();

    public IReadOnlyList<IReadOnlyCellDefinition> CellTypes => ModifiableCellTypes;

    /// <summary>
    ///   The scale in meters of the species
    /// </summary>
    public float Scale { get; set; } = 1.0f;

    public float BrainPower { get; private set; }

    public float MuscularPower { get; private set; }

    /// <summary>
    ///   Where this species reproduces, used to control also where individuals of this species spawn and where the
    ///   player spawns
    /// </summary>
    public ReproductionLocation ReproductionLocation { get; set; }

    public MacroscopicSpeciesType MacroscopicType { get; private set; }

    /// <summary>
    ///   All organelles in all the species' placed metaballs (there can be a lot of duplicates in this list)
    /// </summary>
    public IEnumerable<OrganelleTemplate> Organelles =>
        ((MetaballLayout<MacroscopicMetaball>)ModifiableBodyLayout).Select(m => m.ModifiableCellType).Distinct()
        .SelectMany(c => c.ModifiableOrganelles);

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MacroscopicSpecies;

    public static MacroscopicSpeciesType CalculateMacroscopicTypeFromLayout(MetaballLayout<MacroscopicMetaball> layout,
        float scale)
    {
        var brainPower = CalculateBrainPowerFromLayout(layout, scale);

        if (brainPower >= Constants.BRAIN_POWER_REQUIRED_FOR_AWAKENING)
        {
            return MacroscopicSpeciesType.Awakened;
        }

        if (brainPower >= Constants.BRAIN_POWER_REQUIRED_FOR_AWARE)
        {
            return MacroscopicSpeciesType.Aware;
        }

        return MacroscopicSpeciesType.Macroscopic;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        throw new NotImplementedException();
    }

    public override void OnEdited()
    {
        base.OnEdited();

        RepositionToOrigin();
        UpdateInitialCompounds();
        CalculateBrainPower();
        CalculateMuscularPower();

        // Note that a few stage transitions are explicit for the player, so the editor will override this
        SetTypeFromBrainPower();

        // Probably don't need to reset endosymbiont status here any more as it is likely not possible to perform it
        // at this stage
    }

    public override bool RepositionToOrigin()
    {
        return ModifiableBodyLayout.RepositionToGround();
    }

    public override void UpdateInitialCompounds()
    {
        // TODO: change this to be dynamic similar to microbe stage

        var simulation = SimulationParameters.Instance;

        var rusticyanin = simulation.GetOrganelleType("rusticyanin");
        var chemo = simulation.GetOrganelleType("chemoplast");
        var chemoProtein = simulation.GetOrganelleType("chemoSynthesizingProteins");

        if (Organelles.Any(o => o.Definition == rusticyanin))
        {
            SetInitialCompoundsForIron();
        }
        else if (Organelles.Any(o => o.Definition == chemo ||
                     o.Definition == chemoProtein))
        {
            SetInitialCompoundsForChemo();
        }
        else
        {
            SetInitialCompoundsForDefault();
        }
    }

    public override void HandleNightSpawnCompounds(CompoundBag targetStorage, ISpawnEnvironmentInfo spawnEnvironment)
    {
        // TODO: implement something here if required (probably needed for plants at least if they use this class)
    }

    public override void ApplyMutation(Species mutation)
    {
        base.ApplyMutation(mutation);

        var casted = (MacroscopicSpecies)mutation;

        ModifiableCellTypes.Clear();

        foreach (var cellType in casted.ModifiableCellTypes)
        {
            ModifiableCellTypes.Add((CellType)cellType.Clone());
        }

        ModifiableBodyLayout.Clear();

        var metaballMapping = new Dictionary<Metaball, MacroscopicMetaball>();

        // Make sure we process things with parents first
        // TODO: if the tree depth calculation is too expensive here, we'll need to cache the values in the metaball
        // objects
        foreach (var metaball in ((MetaballLayout<MacroscopicMetaball>)casted.ModifiableBodyLayout).OrderBy(m =>
                     m.CalculateTreeDepth()))
        {
            ModifiableBodyLayout.Add(metaball.Clone(metaballMapping));
        }
    }

    public override float GetPredationTargetSizeFactor()
    {
        throw new NotImplementedException("Size factor for auto-evo not implemented for macroscopic species");
    }

    /// <summary>
    ///   Explicitly moves the player to awakened status, this is like this to make sure the player wouldn't get stuck
    ///   underwater if they accidentally increased their brain power
    /// </summary>
    public void MovePlayerToAwakenedStatus()
    {
        MacroscopicType = MacroscopicSpeciesType.Awakened;
    }

    public void KeepPlayerInAwareStage()
    {
        if (MacroscopicType == MacroscopicSpeciesType.Awakened)
            MacroscopicType = MacroscopicSpeciesType.Aware;
    }

    public override object Clone()
    {
        var result = new MacroscopicSpecies(ID, Genus, Epithet);

        ClonePropertiesTo(result);

        foreach (var cellType in ModifiableCellTypes)
        {
            result.ModifiableCellTypes.Add((CellType)cellType.Clone());
        }

        var metaballMapping = new Dictionary<Metaball, MacroscopicMetaball>();

        foreach (var metaball in (MetaballLayout<MacroscopicMetaball>)ModifiableBodyLayout)
        {
            result.ModifiableBodyLayout.Add(metaball.Clone(metaballMapping));
        }

        return result;
    }

    private static float CalculateBrainPowerFromLayout(MetaballLayout<MacroscopicMetaball> layout, float scale)
    {
        float result = 0;

        foreach (var metaball in layout)
        {
            if (metaball.ModifiableCellType.IsBrainTissueType())
            {
                // TODO: check that volume scaling in physically sensible way (using GetVolume) is what we want here
                // Maybe we would actually just want to multiply by the scale number to buff small species' brain?
                result += metaball.GetVolume(scale);
            }
        }

        return result;
    }

    private static float CalculateMuscularPowerFromLayout(MetaballLayout<MacroscopicMetaball> layout, float scale)
    {
        float result = 0;

        foreach (var metaball in layout)
        {
            if (metaball.ModifiableCellType.IsMuscularTissueType())
            {
                // TODO: check that volume scaling in physically sensible way (using GetVolume) is what we want here
                result += metaball.GetVolume(scale);
            }
        }

        return result;
    }

    private void SetTypeFromBrainPower()
    {
        MacroscopicType = CalculateMacroscopicTypeFromLayout(ModifiableBodyLayout, Scale);
    }

    private void SetInitialCompoundsForDefault()
    {
        InitialCompounds.Clear();

        // TODO: modify these numbers based on the scale and metaball count or something more accurate
        InitialCompounds.Add(Compound.ATP, 180);
        InitialCompounds.Add(Compound.Glucose, 90);
    }

    private void SetInitialCompoundsForIron()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(Compound.Iron, 90);
    }

    private void SetInitialCompoundsForChemo()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(Compound.Hydrogensulfide, 90);
    }

    private void CalculateBrainPower()
    {
        BrainPower = CalculateBrainPowerFromLayout(ModifiableBodyLayout, Scale);
    }

    private void CalculateMuscularPower()
    {
        MuscularPower = CalculateMuscularPowerFromLayout(ModifiableBodyLayout, Scale);
    }
}
