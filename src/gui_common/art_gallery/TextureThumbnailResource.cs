using System;
using Godot;

/// <summary>
///   Loading task for a texture thumbnail
/// </summary>
/// <remarks>
///   <para>
///     TODO: this currently can't resize textures as they are loaded in a way that the object doesn't support resizing
///     as such this can't save memory when showing a ton of thumbnails. As an alternative we could maybe pre-generate
///     thumbnails for required textures.
///   </para>
/// </remarks>
public class TextureThumbnailResource : ITextureResource
{
    private readonly string texturePath;

    // See the TODO comment on this class
    // ReSharper disable once NotAccessedField.Local
    private readonly int thumbnailWidth;

    private Texture2D? loadedTexture;

    public TextureThumbnailResource(string texturePath, int thumbnailWidth)
    {
        this.texturePath = texturePath;
        this.thumbnailWidth = thumbnailWidth;
    }

    // TODO: background loading once Godot supports it
    public bool RequiresSyncLoad => true;
    public bool UsesPostProcessing => false;
    public bool RequiresSyncPostProcess => true;
    public float EstimatedTimeRequired => 0.015f;
    public bool LoadingPrepared { get; set; }
    public bool Loaded { get; private set; }
    public string Identifier => $"Thumbnail/{texturePath}";
    public Action<IResource>? OnComplete { get; set; }

    public Texture2D LoadedTexture => loadedTexture ?? throw new InstanceNotLoadedYetException();

    public void PrepareLoading()
    {
    }

    public void Load()
    {
        loadedTexture = GD.Load<CompressedTexture2D>(texturePath);
        Loaded = true;
    }

    public void PerformPostProcessing()
    {
    }
}
