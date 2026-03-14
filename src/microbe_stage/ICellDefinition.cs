using System.Collections.Generic;
using Godot;

/// <summary>
///   Generic interface to allow working with microbe species and also multicellular species' individual cell types
/// </summary>
/// <remarks>
///   <para>
///     This used to be named ICellProperties but that was very closely named to
///     <see cref="Components.CellProperties"/> so this was renamed to reflect how this is used as a generic
///     information adapter on a cell type definition (similarly to <see cref="OrganelleDefinition"/>).
///   </para>
/// </remarks>
public interface ICellDefinition : IReadOnlyCellDefinition, ISimulationPhotographable
{
    public OrganelleLayout<OrganelleTemplate> ModifiableOrganelles { get; }
    public new MembraneType MembraneType { get; set; }
    public new float MembraneRigidity { get; set; }
    public new Color Colour { get; set; }
    public new bool IsBacteria { get; set; }

    public float BaseRotationSpeed { get; set; }

    /// <summary>
    ///   Returns true if this cell fills all the requirements needed to engulf.
    /// </summary>
    public bool CanEngulf { get; }

    public string FormattedName { get; }

    /// <summary>
    ///   Repositions the cell to the origin and recalculates any properties dependent on its position.
    /// </summary>
    /// <returns>True when changes were made, false if everything was positioned well already</returns>
    public bool RepositionToOrigin();

    public void UpdateNameIfValid(string newName);
}

public interface IReadOnlyCellDefinition
{
    public IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate> Organelles { get; }
    public MembraneType MembraneType { get; }
    public float MembraneRigidity { get; }
    public Color Colour { get; }
    public bool IsBacteria { get; }
}

public interface ICellTypeDefinition : ICellDefinition, IReadOnlyCellTypeDefinition
{
    public new int MPCost { get; set; }
}

public interface IReadOnlyCellTypeDefinition : IReadOnlyCellDefinition, IPlayerReadableName
{
    public int MPCost { get; }

    public string CellTypeName { get; }

    /// <summary>
    ///   If known from what cell type this cell was split from, this is the name of that type.
    /// </summary>
    public string? SplitFromTypeName { get; }

    /// <summary>
    ///   A multiplier starting from 1 and going up based on how specialized this cell type is. This is eventually
    ///   applied to <see cref="Components.BioProcesses.OverallSpeedModifier"/>
    /// </summary>
    public float SpecializationBonus { get; }
}

/// <summary>
///   General helpers for working with a general <see cref="ICellDefinition"/> type.
///   <see cref="Components.CellPropertiesHelpers"/> are related to ECS component operations.
/// </summary>
public static class GeneralCellPropertiesHelpers
{
    /// <summary>
    ///   The total compounds in the composition of all organelles
    /// </summary>
    public static Dictionary<Compound, float> CalculateTotalComposition(this ICellDefinition definition)
    {
        var result = new Dictionary<Compound, float>();

        foreach (var organelle in definition.Organelles)
        {
            result.Merge(organelle.Definition.InitialComposition);
        }

        return result;
    }

    /// <summary>
    ///   The total compounds in the composition of all organelles
    /// </summary>
    public static List<(Compound Compound, float Amount)> CalculateTotalCompositionList(this ICellDefinition definition)
    {
        var result = new List<(Compound Compound, float Amount)>();

        foreach (var organelle in definition.Organelles)
        {
            foreach (var pair in organelle.Definition.InitialComposition)
            {
                var index = result.FindIndexByKey(pair.Key);
                if (index != -1)
                {
                    result[index] = (pair.Key, pair.Value + result[index].Amount);
                }
                else
                {
                    result.Add((pair.Key, pair.Value));
                }
            }
        }

        return result;
    }

    public static void SetupWorldEntities(this ICellDefinition definition, IWorldSimulation worldSimulation)
    {
        // TODO: would there be a way to avoid this temporary memory allocation? This gets used each time the
        // photo studio wants to photograph a cell
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        var species = new MicrobeSpecies(new MicrobeSpecies(int.MaxValue, string.Empty, string.Empty), definition,
            workMemory1, workMemory2)
        {
            // For visualization the bonus doesn't matter, but we need to set a valid value
            SpecializationBonus = 1,
        };

        species.SetupWorldEntities(worldSimulation);
    }

    public static Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return ((MicrobeVisualOnlySimulation)worldSimulation).CalculateMicrobePhotographDistance();
    }
}
