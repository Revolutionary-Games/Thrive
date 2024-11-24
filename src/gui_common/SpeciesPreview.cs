using Godot;

/// <summary>
///   A visual preview of how a species looks like in-game
/// </summary>
public partial class SpeciesPreview : PhotographablePreview
{
    private Species? previewSpecies;

    public Species? PreviewSpecies
    {
        get => previewSpecies;
        set
        {
            if (PreviewSpecies == value)
                return;

            previewSpecies = value;

            if (previewSpecies != null)
                UpdatePreview();
            else
                ResetPreview();
        }
    }

    protected override ImageTask? SetupImageTask()
    {
        if (previewSpecies is MicrobeSpecies microbeSpecies)
        {
            return PhotoStudio.Instance.GenerateImage(microbeSpecies, Priority, KeepPlainImageInMemory);
        }

        GD.PrintErr("Unknown species type to preview: ", previewSpecies);
        return null;
    }
}
