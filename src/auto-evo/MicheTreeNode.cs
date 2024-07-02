using Godot;

/// <summary>
///   MicheTreeNode represents a selectable node for a Miche
///   in <see cref="MicheTree"/>
/// </summary>
public partial class MicheTreeNode : TextureButton
{
    public float Width = 0;
    public int Depth = 0;
    public int MicheHash;

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

    private bool unoccupied;

    public bool Unoccupied
    {
        get => unoccupied;
        set
        {
            if (unoccupied == value)
                return;

            unoccupied = value;

            UpdateTexture();
        }
    }

    public MicheTreeNode? ParentNode { get; set; }

    /// <summary>
    ///   Logical position this node is at in the evolutionary tree.
    /// </summary>
    public Vector2 LogicalPosition { get; set; }

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
        if (Unoccupied)
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
