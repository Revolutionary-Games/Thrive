using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A list of positioned organelles. Verifies that they don't overlap
/// </summary>
/// <typeparam name="T">The type of organelle contained in this layout</typeparam>
[UseThriveSerializer]
public class CellLayout<T> : HexLayout<T>
    where T : class, IPositionedCell
{
    public CellLayout(Action<T> onAdded, Action<T>? onRemoved = null) : base(onAdded, onRemoved)
    {
    }

    public CellLayout()
    {
    }

    // TODO: remove if this doesn't end up being necessary
    /*[JsonIgnore]
    public IReadOnlyList<T> Cells => existingHexes;*/

    /// <summary>
    ///   The center of mass of the contained organelles in all cells
    /// </summary>
    [JsonIgnore]
    public Hex CenterOfMass
    {
        get
        {
            float totalMass = 0;
            Vector3 weightedSum = Vector3.Zero;
            foreach (var organelle in existingHexes.SelectMany(c => c.Organelles))
            {
                totalMass += organelle.Definition.Mass;
                weightedSum += Hex.AxialToCartesian(organelle.Position) * organelle.Definition.Mass;
            }

            return Hex.CartesianToAxial(weightedSum / totalMass);
        }
    }

    protected override IEnumerable<Hex> GetHexComponentPositions(T hex)
    {
        return hex.Organelles.SelectMany(o => o.Definition.GetRotatedHexes(o.Orientation))
            .Select(h => Hex.RotateAxialNTimes(h, hex.Orientation) + hex.Position);
    }
}
