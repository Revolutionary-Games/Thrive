﻿public class CellHexesPreview : PhotographablePreview
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
        return new ImageTask(new CellHexesPhotoBuilder { Species = microbeSpecies }, KeepPlainImageInMemory);
    }
}
