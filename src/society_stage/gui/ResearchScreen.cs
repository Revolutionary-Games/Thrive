using System;
using Godot;

/// <summary>
///   Screen for showing the player's technology options in the strategy stages
/// </summary>
public partial class ResearchScreen : CustomWindow
{
    [Export]
    public NodePath? TechWebGUIPath;

    [Export]
    public NodePath CurrentResearchProgressLabelPath = null!;

#pragma warning disable CA2213
    private TechWebGUI techWebGUI = null!;

    private Label currentResearchProgressLabel = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnStartResearchingEventHandler(string technology);

    public TechWeb? AvailableTechnologies { get; set; }

    public override void _Ready()
    {
        base._Ready();

        techWebGUI = GetNode<TechWebGUI>(TechWebGUIPath);
        currentResearchProgressLabel = GetNode<Label>(CurrentResearchProgressLabelPath);
    }

    public void DisplayProgress(TechnologyProgress? currentResearch)
    {
        if (currentResearch == null)
        {
            currentResearchProgressLabel.Text = Localization.Translate("CURRENT_RESEARCH_NONE");
            return;
        }

        var progressPercentage = Math.Round(currentResearch.OverallProgress * 100, 1);

        currentResearchProgressLabel.Text = Localization.Translate("CURRENT_RESEARCH_PROGRESS")
            .FormatSafe(currentResearch.Technology.Name,
                Localization.Translate("PERCENTAGE_VALUE").FormatSafe(progressPercentage));
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
            if (TechWebGUIPath != null)
            {
                TechWebGUIPath.Dispose();
                CurrentResearchProgressLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ForwardStartResearch(string technology)
    {
        EmitSignal(SignalName.OnStartResearching, technology);
    }
}
