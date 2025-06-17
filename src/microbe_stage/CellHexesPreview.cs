using System;
using Godot;

/// <summary>
///   Shows a visualization of a cell's hexes in the GUI
/// </summary>
public partial class CellHexesPreview : PhotographablePreview
{
    private Species? species;

    public Species? PreviewSpecies
    {
        get => species;
        set
        {
            if (PreviewSpecies == value)
                return;

            species = value;

            if (species != null)
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
        if (species == null)
            throw new InvalidOperationException("No species set to generate image of hexes for");

        if (species is MicrobeSpecies microbeSpecies)
        {
            var hash = CellHexesPhotoBuilder.GetVisualHash(microbeSpecies);

            var task = PhotoStudio.Instance.TryGetFromCache(hash);

            if (task != null)
                return task;

            return PhotoStudio.Instance.GenerateImage(new CellHexesPhotoBuilder { Species = microbeSpecies }, Priority);
        }

        if (species is MulticellularSpecies multicellularSpecies)
        {
            var hash = ColonyHexPhotoBuilder.GetVisualHash(multicellularSpecies);

            var task = PhotoStudio.Instance.TryGetFromCache(hash);

            if (task != null)
                return task;

            if (multicellularSpecies.EditorCellLayout == null)
            {
                GD.PrintErr("No cell layout is remembered, the hex preview can't be generated");
                return null;
            }

            return PhotoStudio.Instance.GenerateImage(new ColonyHexPhotoBuilder { Species = multicellularSpecies },
                Priority);
        }

        GD.PrintErr("Unknown species type to preview: ", species, " (", species.GetType().Name, ")");
        return null;
    }
}
