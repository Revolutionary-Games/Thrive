using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A list of positioned cells. Verifies that they don't overlap
/// </summary>
/// <remarks>
///   <para>
///     This is a JSON reference so that <see cref="Components.MulticellularGrowth"/> can reference this from a
///     species.
///   </para>
/// </remarks>
/// <typeparam name="T">The type of organelle contained in this layout</typeparam>
[UseThriveSerializer]
[JsonObject(IsReference = true)]
public class CellLayout<T> : HexLayout<T>
    where T : class, IPositionedCell
{
    public CellLayout(Action<T> onAdded, Action<T>? onRemoved = null) : base(onAdded, onRemoved)
    {
    }

    public CellLayout()
    {
    }

    [JsonConstructor]
    public CellLayout(List<T> existingHexes, Action<T>? onAdded = null, Action<T>? onRemoved = null) : base(
        existingHexes, onAdded, onRemoved)
    {
    }

    /// <summary>
    ///   The center of mass of the contained organelles in all cells
    /// </summary>
    [JsonIgnore]
    public Hex CenterOfMass
    {
        get
        {
            // TODO: with the new physics its no longer possible to easily calculate the center of mass exactly
            // this instead has to rely on just hex positions (for now). See OrganelleLayout.CenterOfMass
            Vector3 weightedSum = Vector3.Zero;
            int count = 0;
            foreach (var organelle in existingHexes.SelectMany(c => c.Organelles))
            {
                ++count;
                weightedSum += Hex.AxialToCartesian(organelle.Position);
            }

            if (count == 0)
                return new Hex(0, 0);

            return Hex.CartesianToAxial(weightedSum / count);
        }
    }

    protected override void GetHexComponentPositions(T hex, List<Hex> result)
    {
        result.Clear();

        var organellesInternal = hex.Organelles.Organelles;
        int organelleCount = organellesInternal.Count;

        for (int i = 0; i < organelleCount; ++i)
        {
            var organelle = organellesInternal[i];

            var organelleHexes = organelle.Definition.GetRotatedHexes(organelle.Orientation);
            int hexCount = organelleHexes.Count;

            for (int j = 0; j < hexCount; ++j)
            {
                result.Add(Hex.RotateAxialNTimes(organelleHexes[j], hex.Orientation) + organelle.Position);
            }
        }
    }
}
