using System;
using Godot;

/// <summary>
///   Displays a preview image for a cell type
/// </summary>
public partial class CellTypePreview : PhotographablePreview
{
    private ulong cellTypeVisualHash;

    private CellType? cellType;

    public CellType? PreviewCellType
    {
        get => cellType;
        set
        {
            var newHash = value?.GetVisualHashCode() ?? 0UL;

            if (newHash == cellTypeVisualHash)
                return;

            cellType = value;
            cellTypeVisualHash = newHash;

            if (cellType != null)
            {
                UpdatePreview();
            }
            else
            {
                ResetPreview();
            }
        }
    }

    protected override IImageTask? SetupImageTask()
    {
        if (PreviewCellType == null)
        {
            GD.PrintErr("No cell type set to preview, can't create image task");
            return null;
        }

        return PhotoStudio.Instance.GenerateImage(PreviewCellType);
    }
}
