using Godot;

/// <summary>
///   EvolutionaryTreeNode represents a selectable node where a species come about, mutates, or extincts
///   in <see cref="EvolutionaryTree"/>
/// </summary>
public partial class EvolutionaryTreeNode : TextureButton
{
#pragma warning disable CA2213
    private Texture2D unpressedNormalGreen = null!;
    private Texture2D unpressedHoveredGreen = null!;
    private Texture2D pressedNormalGreen = null!;

    private Texture2D unpressedNormalRed = null!;
    private Texture2D unpressedHoveredRed = null!;
    private Texture2D pressedNormalRed = null!;
#pragma warning restore CA2213

    // Due to the fact that Godot doesn't have a pressed hover texture builtin, this is commented out.
    // private Texture pressedHoveredGreen = null!;
    // private Texture pressedHoveredRed = null!;

    private bool lastGeneration;

    public int Generation { get; set; }

    public uint SpeciesID { get; set; }

    public bool LastGeneration
    {
        get => lastGeneration;
        set
        {
            if (lastGeneration == value)
                return;

            lastGeneration = value;

            UpdateTexture();
        }
    }

    public EvolutionaryTreeNode? ParentNode { get; set; }

    /// <summary>
    ///   The internal position in a <see cref="EvolutionaryTree"/>
    /// </summary>
    public Vector2 Position { get; set; }

    public Vector2 Center => Position + Size / 2;

    public override void _Ready()
    {
        base._Ready();

        unpressedNormalGreen = GD.Load<Texture2D>("res://assets/textures/gui/bevel/RoundButtonGreen.png");
        unpressedHoveredGreen = GD.Load<Texture2D>("res://assets/textures/gui/bevel/RoundButtonGreenHover.png");
        pressedNormalGreen = GD.Load<Texture2D>("res://assets/textures/gui/bevel/RoundButtonGreenPressed.png");
        unpressedNormalRed = GD.Load<Texture2D>("res://assets/textures/gui/bevel/RoundButtonRed.png");
        unpressedHoveredRed = GD.Load<Texture2D>("res://assets/textures/gui/bevel/RoundButtonRedHover.png");
        pressedNormalRed = GD.Load<Texture2D>("res://assets/textures/gui/bevel/RoundButtonRedPressed.png");

        UpdateTexture();
    }

    private void UpdateTexture()
    {
        if (lastGeneration)
        {
            TextureNormal = unpressedNormalRed;
            TextureHover = unpressedHoveredRed;
            TexturePressed = pressedNormalRed;
        }
        else
        {
            TextureNormal = unpressedNormalGreen;
            TextureHover = unpressedHoveredGreen;
            TexturePressed = pressedNormalGreen;
        }
    }
}
