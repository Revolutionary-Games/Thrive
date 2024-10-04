using System;
using Godot;

/// <summary>
///   Shows a visualization of a cell's hexes in the GUI
/// </summary>
public partial class CellHexesPreview : PhotographablePreview
{
    private MicrobeSpecies? microbeSpecies;

    public MicrobeSpecies? PreviewSpecies
    {
        get => microbeSpecies;
        set
        {
            if (PreviewSpecies == value)
                return;

            microbeSpecies = value;
            UpdatePreview();
        }
    }

    protected override ImageTask SetupImageTask()
    {
        if (microbeSpecies == null)
            throw new InvalidOperationException("No species set to generate image of hexes for");

        var hash = CellHexesPhotoBuilder.GetVisualHash(microbeSpecies);

        var task = PhotoStudio.Instance.TryGetFromCache(hash);

        if (task != null)
        {
            if (KeepPlainImageInMemory && !task.WillStorePlainImage)
                GD.PrintErr("Already existing task doesn't have store plain image enabled like this preview wants");

            return task;
        }

        if (KeepPlainImageInMemory)
            throw new NotSupportedException("This option hasn't been implemented to be passed");

        return PhotoStudio.Instance.GenerateImage(new CellHexesPhotoBuilder { Species = microbeSpecies }, Priority);
    }
}
