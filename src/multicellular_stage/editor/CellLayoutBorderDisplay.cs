using Godot;
using System;
using System.Collections.Generic;

/// <summary>
///   Uses plane primitives to display cell borders in the multicellular layout editor.
/// </summary>
public partial class CellLayoutBorderDisplay : MultiMeshInstance3D
{
    /// <summary>
    ///   Contains the transforms of border objects (plane primitives) corresponding to each cell type
    /// </summary>
    private Dictionary<CellType, List<Transform3D>> calculatedTransforms = new();

    private Stack<List<Transform3D>> workMemory = new();

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

    public void HideBorders()
    {
        Multimesh.InstanceCount = 0;
    }

    public void DisplayCellLayoutBorders(IReadOnlyList<HexWithData<CellTemplate>> layout)
    {
        foreach (var pair in calculatedTransforms)
        {
            pair.Value.Clear();
            workMemory.Push(pair.Value);
        }

        calculatedTransforms.Clear();

        var mesh = Multimesh;
        int instanceCount = 0;

        int i = 0;
        foreach (var cell in layout)
        {
            var cellType = cell.Data!.ModifiableCellType;

            if (!calculatedTransforms.ContainsKey(cellType))
            {
                var transforms = CalculateBorderObjectTransforms(cellType);
                calculatedTransforms.Add(cellType, transforms);
                instanceCount += transforms.Count;
            }
            else
            {
                instanceCount += calculatedTransforms[cellType].Count;
            }
        }

        mesh.InstanceCount = instanceCount;

        foreach (var cell in layout)
        {
            var cellType = cell.Data!.ModifiableCellType;

            var transforms = calculatedTransforms[cellType];

            foreach (var transform in transforms)
            {
                var cellTransform = new Transform3D(basisRotatedToHexSide[OrientationToHexSide(cell.Orientation)],
                    Hex.AxialToCartesian(cell.Position));

                mesh.SetInstanceTransform(i, cellTransform * transform);
                ++i;
            }
        }
    }

    /// <summary>
    ///   Display a single cell's borders. Removes all other borders. Use this on a separate object to display a
    ///   hovering cell.
    /// </summary>
    public void DisplaySingleCellBorder(HexWithData<CellTemplate> cell, Hex position)
    {
        // TBD: cache ts
        var transforms = CalculateBorderObjectTransforms(cell.Data!.ModifiableCellType);

        var mesh = Multimesh;
        mesh.InstanceCount = transforms.Count;

        int i = 0;
        foreach (var transform in transforms)
        {
            var cellTransform = new Transform3D(basisRotatedToHexSide[OrientationToHexSide(cell.Orientation)],
                Hex.AxialToCartesian(position));

            mesh.SetInstanceTransform(i, cellTransform * transform);
            ++i;
        }
    }

    private List<Transform3D> CalculateBorderObjectTransforms(CellType cell)
    {
        List<Transform3D> list;

        if (workMemory.Count > 0)
        {
            list = workMemory.Pop();
        }
        else
        {
            list = new List<Transform3D>();
        }

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

    private Hex.HexSide OrientationToHexSide(int orientation)
    {
        return (Hex.HexSide)((-orientation).PositiveModulo(6) + 1);
    }
}
