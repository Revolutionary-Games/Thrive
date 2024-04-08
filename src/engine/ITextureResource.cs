using Godot;

/// <summary>
///   A texture resource that can be loaded
/// </summary>
public interface ITextureResource : IResource
{
    public Texture2D LoadedTexture { get; }
}
