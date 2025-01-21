using System;

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

            if (microbeSpecies != null)
            {
                UpdatePreview();
            }
            else
            {
                ResetPreview();
            }
        }
    }

    protected override IImageTask SetupImageTask()
    {
        if (microbeSpecies == null)
            throw new InvalidOperationException("No species set to generate image of hexes for");

        var hash = CellHexesPhotoBuilder.GetVisualHash(microbeSpecies);

        var task = PhotoStudio.Instance.TryGetFromCache(hash);

        if (task != null)
            return task;

        return PhotoStudio.Instance.GenerateImage(new CellHexesPhotoBuilder { Species = microbeSpecies }, Priority);
    }
}
