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
        return new ImageTask(new CellHexPhotoBuilder { Species = microbeSpecies });
    }
}
