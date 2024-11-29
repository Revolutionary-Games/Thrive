using System;
using Godot;

/// <summary>
///   A request for some game object to be generated an image of for <see cref="PhotoStudio"/>, this will complete
///   (or fail) at some point in the future
/// </summary>
public interface IImageTask : ISavableCacheItem
{
    /// <summary>
    ///   Access to the final image
    /// </summary>
    /// <exception cref="InvalidOperationException">If not <see cref="ICacheItem.Finished"/> yet</exception>
    public ImageTexture FinalImage { get; }

    /// <summary>
    ///   Raw <see cref="Image"/> version of <see cref="FinalImage"/>, always stored once ready
    /// </summary>
    /// <exception cref="InvalidOperationException">If not ready</exception>
    public Image PlainImage { get; }

    /// <summary>
    ///   Cache path this is to be saved at. Cannot be saved if null.
    /// </summary>
    public string? CachePath { get; set; }
}
