using System.Collections.Generic;
using Godot;

/// <summary>
///   Generic interface to allow working with microbe species and also multicellular species' individual cell types
/// </summary>
public interface ICellProperties
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
    ///   Calculates the rotation speed of a cell. This is and <see cref="RepositionToOrigin"/> are separately here
    ///   to allow the cell editor to skip some stuff <see cref="Species.OnEdited"/> does.
    /// </summary>
    public void CalculateRotationSpeed();

    public void RepositionToOrigin();
    public void UpdateNameIfValid(string newName);
}

public static class CellPropertiesHelpers
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
}
