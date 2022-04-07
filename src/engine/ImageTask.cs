using System;
using Godot;

/// <summary>
///   A request for some game object to be generated an image of for <see cref="PhotoStudio"/>
/// </summary>
public class ImageTask
{
    private ImageTexture? finalImage;

    public ImageTask(IPhotographable photographable)
    {
        Photographable = photographable;
    }

    public IPhotographable Photographable { get; }

    public bool Finished { get; private set; }

    public ImageTexture FinalImage
    {
        get => finalImage ?? throw new InvalidOperationException("Not finished yet");
        private set => finalImage = value;
    }

    public void OnFinished(ImageTexture texture)
    {
        if (Finished)
            throw new InvalidOperationException("Already finished");

        Finished = true;
        FinalImage = texture;
    }
}
