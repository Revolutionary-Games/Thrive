using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;

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
public class CellLayout<T> : HexLayout<T>, IReadOnlyCellLayout<T>, IArchivable
    where T : class, IPositionedCell
{
    public CellLayout(Action<T> onAdded, Action<T>? onRemoved = null) : base(onAdded, onRemoved)
    {
    }

    public CellLayout()
    {
    }

    protected CellLayout(List<T> existingHexes, Action<T>? onAdded = null, Action<T>? onRemoved = null) : base(
        existingHexes, onAdded, onRemoved)
    {
    }

    /// <summary>
    ///   The center of mass of the contained organelles in all cells
    /// </summary>
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

    public ushort CurrentArchiveVersion => HexLayoutSerializer.SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ExtendedCellLayout;

    public bool CanBeReferencedInArchive => true;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.ExtendedCellLayout)
            throw new NotSupportedException();

        writer.WriteObject((IArchivable)obj);
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(existingHexes);
        writer.WriteDelegateOrNull(onAdded);
        writer.WriteDelegateOrNull(onRemoved);
    }

    /// <summary>
    ///   A somewhat inefficient sanity check that this is a valid layout with no overlaps
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if cells overlap</exception>
    public void ThrowIfCellsOverlap()
    {
        var seen = new HashSet<Hex>();

        foreach (var positionedCell in existingHexes)
        {
            var organellesInternal = positionedCell.ReadonlyOrganelles.Organelles;
            int organelleCount = organellesInternal.Count;

            for (int i = 0; i < organelleCount; ++i)
            {
                var organelle = organellesInternal[i];

                var organelleHexes = organelle.Definition.GetRotatedHexes(organelle.Orientation);
                int hexCount = organelleHexes.Count;

                for (int j = 0; j < hexCount; ++j)
                {
                    var position = Hex.RotateAxialNTimes(organelleHexes[j], positionedCell.Orientation) +
                        organelle.Position + positionedCell.Position;
                    if (!seen.Add(position))
                    {
                        throw new InvalidOperationException($"Cell {positionedCell} overlaps with another cell");
                    }
                }
            }
        }
    }

    protected override void GetHexComponentPositions(T hex, List<Hex> result)
    {
        result.Clear();

        var organellesInternal = hex.ReadonlyOrganelles.Organelles;
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
