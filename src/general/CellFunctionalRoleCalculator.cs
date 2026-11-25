using System.Collections.Generic;
using System.Numerics;

/// <summary>
///   Derives <see cref="CellFunctionalRole"/> values from organelles. This is the single source of truth for what
///   counts as a tissue role, so auto-evo scoring and the tissue composition view cannot drift apart.
/// </summary>
public static class CellFunctionalRoleCalculator
{
    /// <summary>
    ///   Internal name of the organelle that only represents the base cell material.
    /// </summary>
    public const string BaseCellOrganelle = "cytoplasm";

    /// <summary>
    ///   All roles in a stable order for iteration and display purposes.
    /// </summary>
    public static readonly CellFunctionalRole[] AllRoles =
    {
        CellFunctionalRole.Movement,
        CellFunctionalRole.Defense,
        CellFunctionalRole.Sensing,
        CellFunctionalRole.Storage,
        CellFunctionalRole.Photosynthesis,
    };

    /// <summary>
    ///   Roles a cell definition covers. Derived from the organelles so this works for microbe species, multicellular
    ///   cell types and macroscopic tissue without any stored role data.
    /// </summary>
    public static CellFunctionalRole GetTissueRoles(IReadOnlyCellDefinition cell)
    {
        return CalculateFromCell(cell);
    }

    /// <summary>
    ///   Calculates the combined roles of a cell definition (a microbe species or a multicellular cell type).
    /// </summary>
    public static CellFunctionalRole CalculateFromCell(IReadOnlyCellDefinition cell)
    {
        var roles = CellFunctionalRole.None;

        foreach (var organelle in cell.Organelles)
            roles |= CalculateFromOrganelle(organelle.Definition);

        return roles;
    }

    /// <summary>
    ///   Calculates the roles a single organelle type contributes.
    /// </summary>
    public static CellFunctionalRole CalculateFromOrganelle(OrganelleDefinition definition)
    {
        var roles = CellFunctionalRole.None;

        // Cytoplasm is the base cell material every cell has, including its small amount of storage. Counting it would
        // make every cell look like it has a storage tissue and hide the difference between plain cells and cells
        // that actually specialise in storing compounds
        if (definition.InternalName == BaseCellOrganelle)
            return roles;

        // Myofibril is the macroscopic muscle tissue, it must count as movement just like a flagellum does, otherwise
        // the locomotion classification and the muscle tissue checks disagree about the same body
        if (definition.HasMovementComponent || definition.HasCiliaComponent ||
            definition.HasFeatureTag(OrganelleFeatureTag.Myofibril))
        {
            roles |= CellFunctionalRole.Movement;
        }

        if (definition.HasPilusComponent || definition.HasAgentVacuoleComponent)
            roles |= CellFunctionalRole.Defense;

        if (definition.HasChemoreceptorComponent)
            roles |= CellFunctionalRole.Sensing;

        if (definition.Components.Storage != null)
            roles |= CellFunctionalRole.Storage;

        if (UsesSunlight(definition))
            roles |= CellFunctionalRole.Photosynthesis;

        return roles;
    }

    /// <summary>
    ///   Checks whether a cell has nervous tissue, which upstream marks with the axon feature. Kept here so the role
    ///   classification, the nerve calculator and the cell type helpers all use one implementation.
    /// </summary>
    public static bool IsNervousTissue(IReadOnlyCellDefinition cellType)
    {
        return HasFeature(cellType, OrganelleFeatureTag.Axon);
    }

    /// <summary>
    ///   Checks whether any organelle of the cell carries the given feature tag. This is the single place that walks
    ///   organelles looking for a tag, so tissue rules cannot drift apart between callers again.
    /// </summary>
    public static bool HasFeature(IReadOnlyCellDefinition cellType, OrganelleFeatureTag tag)
    {
        foreach (var organelle in cellType.Organelles)
        {
            if (organelle.Definition.HasFeatureTag(tag))
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Number of roles in the given role set, used by scoring code that counts distinct roles.
    /// </summary>
    public static int CountRoles(CellFunctionalRole roles)
    {
        return BitOperations.PopCount((uint)roles);
    }

    /// <summary>
    ///   Lists the individual roles contained in a role set in a stable order.
    /// </summary>
    public static IEnumerable<CellFunctionalRole> Split(CellFunctionalRole roles)
    {
        foreach (var role in AllRoles)
        {
            if ((roles & role) != 0)
                yield return role;
        }
    }

    /// <summary>
    ///   Whether this organelle runs a process that consumes sunlight. Used both by the role classification and by
    ///   the glow binding, so a visual cue cannot drift away from the gameplay definition.
    /// </summary>
    public static bool UsesSunlight(OrganelleDefinition definition)
    {
        var glucose = SimulationParameters.GetCompound(Compound.Glucose);

        foreach (var process in definition.RunnableProcesses)
        {
            var usesSunlight = false;

            foreach (var input in process.Process.Inputs)
            {
                if (input.Key.ID == Compound.Sunlight)
                {
                    usesSunlight = true;
                    break;
                }
            }

            if (usesSunlight && process.Process.Outputs.ContainsKey(glucose))
                return true;
        }

        return false;
    }
}
