using Godot;

/// <summary>
///   A visual preview of how a species looks like in-game
/// </summary>
public partial class SpeciesPreview : PhotographablePreview
{
    private ulong speciesVisualHash;

    private Species? previewSpecies;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            var newHash = value?.GetVisualHashCode() ?? 0UL;

            if (newHash == speciesVisualHash)
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
        {
            GD.PrintErr("No species set to preview, can't create image task");
            return null;
        }

        if (previewSpecies is MicrobeSpecies microbeSpecies)
        {
            return PhotoStudio.Instance.GenerateImage(microbeSpecies, Priority);
        }

        if (previewSpecies is MulticellularSpecies multicellularSpecies)
        {
            return PhotoStudio.Instance.GenerateImage(multicellularSpecies, Priority);
        }

        GD.PrintErr("Unknown species type to preview: ", previewSpecies, " (", previewSpecies.GetType().Name, ")");
        return null;
    }
}
