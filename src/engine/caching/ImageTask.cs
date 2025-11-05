using System;
using Godot;

public class ImageTask : IImageTask
{
    /// <summary>
    ///   This task's priority. The lower the number, the higher the priority.
    /// </summary>
    public readonly int Priority;

    private ImageTexture? finalImage;
    private Image? plainImage;

    public ImageTask(IScenePhotographable photographable, int priority = 1)
    {
        Priority = priority;
        ScenePhotographable = photographable;
    }

    public ImageTask(ISimulationPhotographable photographable, int priority = 1)
    {
        Priority = priority;
        SimulationPhotographable = photographable;
    }

    public bool Finished { get; private set; }

    public ImageTexture FinalImage
    {
        get => finalImage ?? throw new InvalidOperationException("Not finished yet");
        private set => finalImage = value;
    }

    public Image PlainImage
    {
        get => plainImage ?? throw new InvalidOperationException("Not finished yet");
        private set => plainImage = value;
    }

    public string? CachePath { get; set; }

    internal IScenePhotographable? ScenePhotographable { get; }
    internal ISimulationPhotographable? SimulationPhotographable { get; }

    public void OnFinished(ImageTexture texture, Image rawImage)
    {
        if (Finished)
            throw new InvalidOperationException("Already finished");

        FinalImage = texture;
        PlainImage = rawImage;

        Finished = true;
    }

    public ulong CalculateCacheHash()
    {
        return ScenePhotographable?.GetVisualHashCode() ?? SimulationPhotographable?.GetVisualHashCode() ??
            throw new InvalidOperationException("Image task has neither scene or simulation photograph data");
    }

    public void Save()
    {
        var path = CachePath;
        if (path == null)
            throw new InvalidOperationException("Cache path not set");

        FileHelpers.MakeSureParentDirectoryExists(path);
        PlainImage.SavePng(path);
    }
}

/// <summary>
///   A variant of the image task that is handled by loading it from the disk
/// </summary>
public class CacheLoadedImage : IImageTask, ILoadableCacheItem
{
    private readonly ulong hash;
    private readonly string loadPath;

    private ImageTexture? finalImage;
    private Image? plainImage;

    public CacheLoadedImage(ulong hash, string loadPath)
    {
        this.hash = hash;
        this.loadPath = loadPath;
    }

    public bool Finished { get; private set; }

    public ImageTexture FinalImage
    {
        get
        {
            if (finalImage != null)
                return finalImage;

            // If the image is not yet converted, convert it now
            if (plainImage != null)
            {
                return finalImage = ImageTexture.CreateFromImage(plainImage) ??
                    throw new InvalidOperationException("Failed to create texture from image");
            }

            throw new InvalidOperationException("Not loaded yet");
        }
        private set => finalImage = value;
    }

    public Image PlainImage
    {
        get => plainImage ?? throw new InvalidOperationException("Not loaded yet");
        private set => plainImage = value;
    }

    public string? CachePath
    {
        get => loadPath;
        set => throw new NotSupportedException("Loadable cache image must have final path set initially");
    }

    public ulong CalculateCacheHash()
    {
        return hash;
    }

    public void Load()
    {
        plainImage = Image.LoadFromFile(loadPath);

        if (plainImage == null)
        {
            GD.PrintErr("Loading cache item failed");
            throw new Exception("Load failed");
        }

        finalImage = null;

        // TODO: does this need to run on the main thread?
        // This can be removed from here if required as the getter is already setup to allow automatically creating
        // this when required.
        FinalImage = ImageTexture.CreateFromImage(plainImage);

        if (FinalImage == null)
            GD.PrintErr("Converting image into a texture failed in disk loaded image");

        Finished = true;
    }

    public void Unload()
    {
        // Allow some GUI parts to still use the image, so just let go of the reference instead of destroying the data
        plainImage = null;
        finalImage = null;

        Finished = false;
    }

    public void Save()
    {
        FileHelpers.MakeSureParentDirectoryExists(loadPath);
        PlainImage.SavePng(loadPath);
    }
}
