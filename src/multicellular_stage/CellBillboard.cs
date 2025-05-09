﻿using System;
using Godot;

/// <summary>
///   A texture rect that shows an image of a cell. A much more performant approach to displaying a cell's visuals.
/// </summary>
/// <remarks>
///   <para>
///     Note that as currently this is only used in top-down view, this doesn't have the billboard part that this is
///     always rotated towards the camera. That will be needed if this is to be used in some new context.
///   </para>
/// </remarks>
public partial class CellBillboard : Node3D
{
    /// <summary>
    ///   The priority that the generated image tasks will have. The lower the number, the higher the priority.
    /// </summary>
    [Export]
    public int Priority = 1;

    private ulong displayedHash;

    private ICellDefinition? displayedCell;

#pragma warning disable CA2213
    private MeshInstance3D? quad;

    private Texture2D? cellImage;
#pragma warning restore CA2213

    private StandardMaterial3D material = null!;
    private IImageTask? imageTask;

    private bool dirty = true;
    private float scale = Constants.DEFAULT_HEX_SIZE * Constants.CELL_BILLBOARD_DEFAULT_SCALE_MULTIPLIER;

    /// <summary>
    ///   The microbe image to display here
    /// </summary>
    public ICellDefinition? DisplayedCell
    {
        get => displayedCell;
        set
        {
            if (displayedCell == value)
                return;

            // Skip if visual hash stayed the same
            if (displayedCell != null && value != null && displayedHash == value.GetVisualHashCode())
                return;

            displayedCell = value;
            dirty = true;
            imageTask = null;
        }
    }

    /// <summary>
    ///   How big the cell is displayed. Defaults to taking up roughly one hex.
    /// </summary>
    [Export]
    public float CellScale
    {
        get => scale;
        set
        {
            scale = value;
            ApplyScale();
        }
    }

    public override void _Ready()
    {
        quad = GetChild<MeshInstance3D>(0);

        material = new StandardMaterial3D
        {
            AlbedoTexture = null,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        quad.MaterialOverride = material;

        // Make the quad not visible before it has a material to prevent it from flashing white
        quad.Visible = false;

        ApplyScale();
    }

    public override void _Process(double delta)
    {
        if (!dirty)
            return;

        if (quad == null)
            throw new NotSupportedException("Billboard was not initialized");

        // TODO: should this skip starting generating the image while this is hidden? (maybe once we have image request
        // caching for microbes this will not matter at all)

        if (imageTask != null)
        {
            if (imageTask.Finished)
            {
                cellImage = imageTask.FinalImage;
                material.AlbedoTexture = cellImage;
                quad.Visible = true;
            }

            return;
        }

        if (displayedCell == null)
        {
            // This was cleared
            quad.Visible = false;
            material.AlbedoTexture = null;
        }
        else
        {
            StartGeneratingImage();
        }
    }

    /// <summary>
    ///   Call if the <see cref="DisplayedCell"/> type has changed since being passed here. For performance reasons
    ///   this doesn't check each frame if the cell properties have changed.
    /// </summary>
    public void NotifyCellTypeMayHaveChanged()
    {
        if (displayedCell == null)
            return;

        if (displayedHash != displayedCell.GetVisualHashCode())
        {
            dirty = true;
            imageTask = null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (material != null!)
                material.Dispose();
        }

        base.Dispose(disposing);
    }

    private void StartGeneratingImage()
    {
        if (displayedCell == null)
            throw new InvalidOperationException("Needs to have cell type to display set");

        displayedHash = displayedCell.GetVisualHashCode();

        imageTask = PhotoStudio.Instance.GenerateImage(displayedCell, Priority);

        // Show previous graphics while generating the new image (don't reset to null). As with quick generations this
        // would flicker some.
    }

    private void ApplyScale()
    {
        if (quad == null)
            return;

        quad.Scale = new Vector3(scale, scale, scale);
    }
}
