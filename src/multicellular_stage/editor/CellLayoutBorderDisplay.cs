using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Uses plane primitives to display cell borders in the multicellular layout editor.
/// </summary>
public partial class CellLayoutBorderDisplay : MultiMeshInstance3D
{
    /// <summary>
    ///   Contains the transforms of border objects (plane primitives) corresponding to cell types.
    /// </summary>
    private Dictionary<CellType, List<Transform3D>> borderTransformCache = new();

    private Stack<List<Transform3D>> workMemory = new();

    private HashSet<Hex> borderDetectionWorkMemory = new();

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

    private bool performedOriginShiftLastTime;

    public void HideBorders()
    {
        Multimesh.InstanceCount = 0;

        // This isn't absolutely necessary, but not clearing the cache here would allow borderTransformCache to grow
        // indefinitely
        ClearCellTypeCache();
    }

    public void DisplayCellLayoutBorders(IReadOnlyList<HexWithData<CellTemplate>> layout, bool performOriginShift,
        Func<CellType, CellType>? getUpdatedCellType = null)
    {
        if (performedOriginShiftLastTime != performOriginShift)
        {
            // CellType's hash doesn't include layout's option to use origin shift, so the cache needs to be cleared
            ClearCellTypeCache();
        }

        performedOriginShiftLastTime = performOriginShift;

        var mesh = Multimesh;
        int borderObjectCount = 0;

        foreach (var cell in layout)
        {
            var cellType = cell.Data!.ModifiableCellType;

            if (getUpdatedCellType != null)
                cellType = getUpdatedCellType(cellType);

            if (!borderTransformCache.TryGetValue(cellType, out var transforms))
            {
                transforms = CalculateBorderObjectTransforms(cellType, performOriginShift);
                borderTransformCache.Add(cellType, transforms);
            }

            borderObjectCount += transforms.Count;
        }

        mesh.InstanceCount = borderObjectCount;

        int i = 0;
        foreach (var cell in layout)
        {
            var cellType = cell.Data!.ModifiableCellType;

            if (getUpdatedCellType != null)
                cellType = getUpdatedCellType(cellType);

            var transforms = borderTransformCache[cellType];

            var cellTransform = new Transform3D(basisRotatedToHexSide[OrientationToHexSide(cell.Orientation)],
                Hex.AxialToCartesian(cell.Position));

            foreach (var borderTransform in transforms)
            {
                mesh.SetInstanceTransform(i, cellTransform * borderTransform);
                ++i;
            }
        }
    }

    /// <summary>
    ///   Display a single cell's borders. Removes all other borders. Use this on a separate instance of this class to
    ///   display a hovering cell.
    /// </summary>
    public void DisplayHoveringCellBorder(HexWithData<CellTemplate> cell, Hex positionOverride, bool performOriginShift,
        Func<CellType, CellType>? getUpdatedCellType = null)
    {
        if (performedOriginShiftLastTime != performOriginShift)
        {
            // CellType's hash doesn't include layout's option to use origin shift, so the cache needs to be cleared
            ClearCellTypeCache();
        }

        performedOriginShiftLastTime = performOriginShift;

        var cellType = cell.Data!.ModifiableCellType;

        if (getUpdatedCellType != null)
            cellType = getUpdatedCellType(cellType);

        if (!borderTransformCache.TryGetValue(cellType, out var transforms))
        {
            transforms = CalculateBorderObjectTransforms(cellType, performOriginShift);
            borderTransformCache.Add(cellType, transforms);
        }

        var mesh = Multimesh;
        mesh.InstanceCount = transforms.Count;

        var cellTransform = new Transform3D(basisRotatedToHexSide[OrientationToHexSide(cell.Orientation)],
            Hex.AxialToCartesian(positionOverride));

        int i = 0;
        foreach (var borderTransform in transforms)
        {
            mesh.SetInstanceTransform(i, cellTransform * borderTransform);
            ++i;
        }
    }

    private List<Transform3D> CalculateBorderObjectTransforms(CellType cell, bool performOriginShift)
    {
        List<Transform3D> list;

        if (workMemory.Count > 0)
        {
            list = workMemory.Pop();
            list.Clear();
        }
        else
        {
            list = new List<Transform3D>();
        }

        borderDetectionWorkMemory.Clear();

        Hex originShift = performOriginShift ?
            MulticellularLayoutHelpers.CalculateExpectedLayoutShift(cell.ModifiableOrganelles) : new Hex(0, 0);

        foreach (var organelle in cell.ModifiableOrganelles)
        {
            foreach (var organelleHex in organelle.Definition.GetRotatedHexes(organelle.Orientation))
            {
                var position = organelleHex + organelle.Position - originShift;
                borderDetectionWorkMemory.Add(position);
            }
        }

        foreach (var hex in borderDetectionWorkMemory)
        {
            var cartesianHexPos = Hex.AxialToCartesian(hex);

            foreach (var pair in Hex.HexNeighbourOffset)
            {
                if (!borderDetectionWorkMemory.Contains(hex + pair.Value))
                {
                    var dir = Hex.AxialToCartesian(pair.Value);

                    list.Add(new Transform3D(basisRotatedToHexSide[pair.Key], cartesianHexPos + dir * 0.5f));
                }
            }
        }

        return list;
    }

    private Hex.HexSide OrientationToHexSide(int orientation)
    {
        return (Hex.HexSide)((-orientation).PositiveModulo(6) + 1);
    }

    private void ClearCellTypeCache()
    {
        foreach (var pair in borderTransformCache)
        {
            pair.Value.Clear();
            workMemory.Push(pair.Value);
        }

        borderTransformCache.Clear();
    }
}
