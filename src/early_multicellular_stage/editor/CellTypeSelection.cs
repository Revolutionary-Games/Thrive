using System;
using Godot;

/// <summary>
///   Handles showing the cell type preview on a selection button
/// </summary>
public class CellTypeSelection : MicrobePartSelection
{
    private CellType? cellType;

    private Texture placeholderIcon = null!;

    private Texture? cellImage;
    private ImageTask? imageTask;

    public CellType CellType
    {
        get => cellType ?? throw new InvalidOperationException("No cell type set");
        set
        {
            ReportTypeChanged();
            cellType = value;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        placeholderIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/IconGenerating.png");

        PartIcon = placeholderIcon;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (cellImage != null)
            return;

        if (imageTask != null)
        {
            if (imageTask.Finished)
            {
                cellImage = imageTask.FinalImage;
                PartIcon = cellImage;
            }

            return;
        }

        // We need to generate a cell image
        imageTask = new ImageTask(CellType);

        PhotoStudio.Instance.SubmitTask(imageTask);

        // Show placeholder while generating
        // TODO: only show placeholder after 10-ish frames if we don't have the real image ready
        PartIcon = placeholderIcon;
    }

    public void ReportTypeChanged()
    {
        cellImage = null;
    }
}
