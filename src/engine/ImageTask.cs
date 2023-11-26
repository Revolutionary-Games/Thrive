using System;
using Godot;

/// <summary>
///   A request for some game object to be generated an image of for <see cref="PhotoStudio"/>
/// </summary>
public class ImageTask
{
    private readonly bool storePlainImage;

    private ImageTexture? finalImage;
    private Image? plainImage;

    public ImageTask(IScenePhotographable photographable, bool storePlainImage = false)
    {
        this.storePlainImage = storePlainImage;
        ScenePhotographable = photographable;
    }

    public ImageTask(ISimulationPhotographable photographable, bool storePlainImage = false)
    {
        this.storePlainImage = storePlainImage;
        SimulationPhotographable = photographable;
    }

    public bool Finished { get; private set; }

    public ImageTexture FinalImage
    {
        get => finalImage ?? throw new InvalidOperationException("Not finished yet");
        private set => finalImage = value;
    }

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
}
