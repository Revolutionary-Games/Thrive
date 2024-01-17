using System.Collections.Generic;
using Godot;

/// <summary>
///   Generic interface to allow working with microbe species and also multicellular species' individual cell types
/// </summary>
public interface ICellProperties : ISimulationPhotographable
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
///   General helpers for working with a general <see cref="ICellProperties"/> type.
///   <see cref="Components.CellPropertiesHelpers"/> are related to ECS component operations.
/// </summary>
public static class GeneralCellPropertiesHelpers
{
    /// <summary>
    ///   The total compounds in the composition of all organelles
    /// </summary>
    public static Dictionary<Compound, float> CalculateTotalComposition(this ICellProperties properties)
    {
        var result = new Dictionary<Compound, float>();

        foreach (var organelle in properties.Organelles)
        {
            result.Merge(organelle.Definition.InitialComposition);
        }

        return result;
    }

    public static void SetupWorldEntities(this ICellProperties properties, IWorldSimulation worldSimulation)
    {
        new MicrobeSpecies(new MicrobeSpecies(int.MaxValue, string.Empty, string.Empty), properties).SetupWorldEntities(
            worldSimulation);
    }

    public static Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return ((MicrobeVisualOnlySimulation)worldSimulation).CalculateMicrobePhotographDistance();
    }

    public static int GetVisualHashCode(this ICellProperties properties)
    {
        int hash = properties.Colour.GetHashCode() * 607;

        hash ^= (properties.MembraneType.GetHashCode() * 5743) ^ (properties.MembraneRigidity.GetHashCode() * 5749) ^
            ((properties.IsBacteria ? 1 : 0) * 5779) ^ (properties.Organelles.Count * 131);

        int counter = 0;
        foreach (var organelle in properties.Organelles)
        {
            hash ^= counter++ * 13 * organelle.GetHashCode();
        }

        return hash;
    }
}
