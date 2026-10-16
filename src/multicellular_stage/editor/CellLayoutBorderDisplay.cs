using Godot;
using System;
using System.Collections.Generic;

/// <summary>
///   Uses plane primitives to display cell borders in the multicellular layout editor.
/// </summary>
public partial class CellLayoutBorderDisplay : MultiMeshInstance3D
{
    private Dictionary<CellType, List<Transform3D>> calculatedTransforms = new();

    public void DisplayCellLayout(IReadOnlyList<CellTemplate> layout)
    {
        calculatedTransforms.Clear();

        var mesh = Multimesh;
        mesh.InstanceCount = 1000; // TBD: Fix this

        int i = 0;
        foreach (var cell in layout)
        {
            if (!calculatedTransforms.TryGetValue(cell.ModifiableCellType, out var transformList))
            {
                transformList = CalculateBorderTransforms(cell.ModifiableCellType);
                calculatedTransforms.Add(cell.ModifiableCellType, transformList);
            }

            foreach (var transform in transformList)
            {
                var cellTransform = new Transform3D(Basis.Identity.Rotated(Vector3.Up, cell.Orientation * MathF.PI * 0.166f), Hex.AxialToCartesian(cell.Position));

                mesh.SetInstanceTransform(i, cellTransform * transform);
                ++i;
            }
        }
    }

    private List<Transform3D> CalculateBorderTransforms(CellType cell)
    {
        var list = new List<Transform3D>();

        var hexes = new HashSet<Hex>();

        foreach (var organelle in cell.ModifiableOrganelles)
        {
            foreach (var organelleHex in organelle.Definition.GetRotatedHexes(organelle.Orientation))
            {
                var position = organelleHex + organelle.Position;
                hexes.Add(position);
            }
        }

        foreach (var hex in hexes)
        {
            foreach (var pair in Hex.HexNeighbourOffset)
            {
                if (!hexes.Contains(hex + pair.Value))
                {
                    // TBD: FIX this code
                    var dir3D = Hex.AxialToCartesian(pair.Value);

                    list.Add(
                        new Transform3D(Basis.LookingAt(dir3D, Vector3.Up), Hex.AxialToCartesian(hex) + dir3D * 0.5f));
                }
            }
        }

        return list;
    }
}
