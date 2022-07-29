public class CellHexPreview : PhotographPreview
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
        var imageTask = new ImageTask(new CellHexPhotoBuilder { Species = microbeSpecies });
        PhotoStudio.Instance.SubmitTask(imageTask);

        return imageTask;
    }
}
