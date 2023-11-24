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

    // TODO: this is a bit expensive property now as this uses MicrobeInternalCalculations.CalculateRotationSpeed which
    // now needs to generate the full physics shape to calculate inertia. Maybe the users of this could be switched
    // to a lazy method to ensure that species generation and modification is faster?
    public float BaseRotationSpeed { get; set; }

    /// <summary>
    ///   Returns true if this cell fills all the requirements needed to engulf.
    /// </summary>
    public bool CanEngulf { get; }

    public string FormattedName { get; }

    /// <summary>
    ///   Repositions the cell to the origin and recalculates any properties dependant on its position.
    /// </summary>
    public void RepositionToOrigin();

    public void UpdateNameIfValid(string newName);
}

public static class CellPropertiesHelpers
{
    // TODO: this can probably be deleted entirely as unused old code
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

    public static void SetupWorldEntities(this ICellProperties properties, IWorldSimulation worldSimulation)
    {
        new MicrobeSpecies(new MicrobeSpecies(int.MaxValue, string.Empty, string.Empty), properties).SetupWorldEntities(
            worldSimulation);
    }

    public static Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return ((MicrobeVisualOnlySimulation)worldSimulation).CalculateMicrobePhotographDistance();
    }
}
