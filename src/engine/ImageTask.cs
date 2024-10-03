using System;
using Godot;

/// <summary>
///   A request for some game object to be generated an image of for <see cref="PhotoStudio"/>
/// </summary>
public class ImageTask : ICacheItem
{
    /// <summary>
    ///   This task's priority. The lower the number, the higher the priority.
    /// </summary>
    public readonly int Priority;

    private readonly bool storePlainImage;

    private ImageTexture? finalImage;
    private Image? plainImage;

    public ImageTask(IScenePhotographable photographable, bool storePlainImage = false, int priority = 1)
    {
        this.storePlainImage = storePlainImage;
        Priority = priority;
        ScenePhotographable = photographable;
    }

    public ImageTask(ISimulationPhotographable photographable, bool storePlainImage = false, int priority = 1)
    {
        this.storePlainImage = storePlainImage;
        Priority = priority;
        SimulationPhotographable = photographable;
    }

    public bool Finished { get; private set; }

    public ImageTexture FinalImage
    {
        get => finalImage ?? throw new InvalidOperationException("Not finished yet");
        private set => finalImage = value;
    }

    public bool WillStorePlainImage => storePlainImage;

    /// <summary>
    ///   Raw <see cref="Image"/> version of <see cref="FinalImage"/>, only stored if specified in the constructor
    /// </summary>
    /// <exception cref="InvalidOperationException">If not ready or configured not</exception>
    public Image PlainImage
    {
        get => plainImage ?? throw new InvalidOperationException("Not finished yet or not configured to be saved");
        private set => plainImage = value;
    }

    internal IScenePhotographable? ScenePhotographable { get; }
    internal ISimulationPhotographable? SimulationPhotographable { get; }

    public void OnFinished(ImageTexture texture, Image rawImage)
    {
        if (Finished)
            throw new InvalidOperationException("Already finished");

        FinalImage = texture;

        if (storePlainImage)
        {
            PlainImage = rawImage;
        }

        Finished = true;
    }

    public ulong CalculateCacheHash()
    {
        return ScenePhotographable?.GetVisualHashCode() ?? SimulationPhotographable?.GetVisualHashCode() ??
            throw new InvalidOperationException("Image task has neither scene or simulation photograph data");
    }
}
