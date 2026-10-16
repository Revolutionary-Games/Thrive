using Godot;
using System;
using System.Collections.Generic;

/// <summary>
///   Uses plane primitives to display cell borders in the multicellular layout editor.
/// </summary>
public partial class CellLayoutBorderDisplay : MultiMeshInstance3D
{
    private Dictionary<CellType, List<Transform3D>> calculatedTransforms = new();

    private Dictionary<Hex.HexSide, Basis> basisRotatedToHexSide =
        new()
        {
            { Hex.HexSide.Top, Basis.Identity },
            { Hex.HexSide.TopRight, Basis.Identity.Rotated(Vector3.Up, MathF.PI * 1 / 3) },
            { Hex.HexSide.BottomRight, Basis.Identity.Rotated(Vector3.Up, MathF.PI * 2 / 3) },
            { Hex.HexSide.Bottom, Basis.Identity.Rotated(Vector3.Up, MathF.PI) },
            { Hex.HexSide.BottomLeft, Basis.Identity.Rotated(Vector3.Up, MathF.PI * 4 / 3) },
            { Hex.HexSide.TopLeft, Basis.Identity.Rotated(Vector3.Up, MathF.PI * 5 / 3) },
        };

    public void DisplayCellLayout(IReadOnlyList<CellTemplate> layout)
    {
        calculatedTransforms.Clear();

        var mesh = Multimesh;
        mesh.InstanceCount = 1000; // TBD: Fix this

        int i = 0;
        foreach (var cell in layout)
        {
            if (!calculatedTransforms.ContainsKey(cell.ModifiableCellType))
            {
                var transforms = CalculateBorderTransforms(cell.ModifiableCellType);
                calculatedTransforms.Add(cell.ModifiableCellType, transforms);
            }
        }

        foreach (var cell in layout)
        {
            var transforms = calculatedTransforms[cell.ModifiableCellType];

            foreach (var transform in transforms)
            {
                var cellTransform = new Transform3D(basisRotatedToHexSide[(Hex.HexSide)(cell.Orientation + 1)], Hex.AxialToCartesian(cell.Position));

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
                    var dir3D = Hex.AxialToCartesian(pair.Value);

                    list.Add(new Transform3D(basisRotatedToHexSide[pair.Key], Hex.AxialToCartesian(hex) + dir3D * 0.5f));
                }
            }
        }

        return list;
    }
}
