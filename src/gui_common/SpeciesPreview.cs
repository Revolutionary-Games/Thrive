using Godot;

public class SpeciesPreview : PhotographPreview
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
        }
    }

    protected override ImageTask? SetupImageTask()
    {
        if (previewSpecies is MicrobeSpecies microbeSpecies)
        {
            var imageTask = new ImageTask(microbeSpecies);
            PhotoStudio.Instance.SubmitTask(imageTask);

            return imageTask;
        }

        GD.PrintErr("Unknown species type to preview: ", previewSpecies);
        return null;
    }
}
