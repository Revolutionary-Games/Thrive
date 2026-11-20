using System;
using Godot;

/// <summary>
///   Shows a visualization of a cell's hexes in the GUI
/// </summary>
public partial class CellHexesPreview : PhotographablePreview
{
    private ulong speciesVisualHash;

    private Species? previewSpecies;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            var newHash = GetVisualHash(value);

            if (newHash == speciesVisualHash && !PreviewFailed)
                return;

            previewSpecies = value;
            speciesVisualHash = newHash;

            if (previewSpecies != null)
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
        if (previewSpecies == null)
            throw new InvalidOperationException("No species set to generate image of hexes for");

        if (previewSpecies is MicrobeSpecies microbeSpecies)
        {
            var hash = CellHexesPhotoBuilder.GetVisualHash(microbeSpecies);

            var task = PhotoStudio.Instance.TryGetFromCache(hash);

            if (task != null)
                return task;

            return PhotoStudio.Instance.GenerateImage(new CellHexesPhotoBuilder { Species = microbeSpecies }, Priority);
        }

        if (previewSpecies is MulticellularSpecies multicellularSpecies)
        {
            var hash = ColonyHexPhotoBuilder.GetVisualHash(multicellularSpecies);

            var task = PhotoStudio.Instance.TryGetFromCache(hash);

            if (task != null)
                return task;

            return PhotoStudio.Instance.GenerateImage(new ColonyHexPhotoBuilder { Species = multicellularSpecies },
                Priority);
        }

        GD.PrintErr("Unknown species type to generate hexes view of: ", previewSpecies, " (",
            previewSpecies.GetType().Name, ")");
        return null;
    }

    private static ulong GetVisualHash(Species? species)
    {
        if (species == null)
            return 0UL;

        if (species is MicrobeSpecies microbeSpecies)
            return CellHexesPhotoBuilder.GetVisualHash(microbeSpecies);

        if (species is MulticellularSpecies multicellularSpecies)
            return ColonyHexPhotoBuilder.GetVisualHash(multicellularSpecies);

        return species.GetVisualHashCode();
    }
}
