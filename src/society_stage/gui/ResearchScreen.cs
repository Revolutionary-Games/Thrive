using Godot;

/// <summary>
///   Screen for showing the player's technology options in the strategy stages
/// </summary>
public class ResearchScreen : CustomDialog
{
    [Export]
    public NodePath? TechWebGUIPath;

#pragma warning disable CA2213
    private TechWebGUI techWebGUI = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnStartResearching(string technology);

    public TechWeb? AvailableTechnologies { get; set; }

    public override void _Ready()
    {
        base._Ready();

        techWebGUI = GetNode<TechWebGUI>(TechWebGUIPath);
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        if (AvailableTechnologies != null)
        {
            techWebGUI.DisplayTechnologies(AvailableTechnologies);
        }
        else
        {
            GD.PrintErr("Available technologies not set for research screen before opening");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TechWebGUIPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    // TODO: hook up this and the HUD's version in Godot editor for this
    private void ForwardStartResearch(string technology)
    {
        EmitSignal(nameof(OnStartResearching), technology);
    }
}
