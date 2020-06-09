using Godot;

/// <summary>
///   A chromatic abberation filter
/// </summary>
public class ChromaticFilter : TextureRect
{
    private static ChromaticFilter instance;

    private ChromaticFilter()
    {
        instance = this;
    }

    public static ChromaticFilter Instance => instance;

    public override void _Ready()
    {
        Show();
    }
}
