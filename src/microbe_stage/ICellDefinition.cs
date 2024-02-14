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
public interface ICellDefinition : ISimulationPhotographable
{
    public OrganelleLayout<OrganelleTemplate> Organelles { get; }
    public MembraneType MembraneType { get; set; }
    public float MembraneRigidity { get; set; }
    public Color Colour { get; set; }
    public bool IsBacteria { get; set; }

    public float BaseRotationSpeed { get; set; }

    /// <summary>
    ///   Returns true if this cell fills all the requirements needed to engulf.
    /// </summary>
    public bool CanEngulf { get; }

    public string FormattedName { get; }

    /// <summary>
    ///   Repositions the cell to the origin and recalculates any properties dependant on its position.
    /// </summary>
    /// <returns>True when changes were made, false if everything was positioned well already</returns>
    public bool RepositionToOrigin();

    public void UpdateNameIfValid(string newName);
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

    public static void SetupWorldEntities(this ICellDefinition definition, IWorldSimulation worldSimulation)
    {
        // TODO: would there be a way to avoid this temporary memory allocation? This gets used each time the
        // photo studio wants to photograph a cell
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        new MicrobeSpecies(new MicrobeSpecies(int.MaxValue, string.Empty, string.Empty), definition, workMemory1,
            workMemory2).SetupWorldEntities(worldSimulation);
    }

    public static Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return ((MicrobeVisualOnlySimulation)worldSimulation).CalculateMicrobePhotographDistance();
    }

    public static int GetVisualHashCode(this ICellDefinition definition)
    {
        int hash = definition.Colour.GetHashCode() * 607;

        hash ^= (definition.MembraneType.GetHashCode() * 5743) ^ (definition.MembraneRigidity.GetHashCode() * 5749) ^
            ((definition.IsBacteria ? 1 : 0) * 5779) ^ (definition.Organelles.Count * 131);

        int counter = 0;
        foreach (var organelle in definition.Organelles)
        {
            hash ^= counter++ * 13 * organelle.GetHashCode();
        }

        return hash;
    }
}
