using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   The compounds panel and the agents panel part of the microbe HUD
/// </summary>
public partial class CompoundPanels : BarPanelBase
{
    private readonly StringName vSeparationReference = new("vseparation");
    private readonly StringName hSeparationReference = new("hseparation");

    private readonly List<CompoundProgressBar> agentsCreatedBars = new();

#pragma warning disable CA2213
    [Export]
    private GridContainer agentsContainer = null!;

    [Export]
    private Control agentsParentContainer = null!;
#pragma warning restore CA2213

    private bool showAgents = true;

    // Needed to determine which animation should be played
    private bool currentAgentsState = true;
    private bool currentCompoundsState = true;

    /// <summary>
    ///   Shows / hides agents panel. Can only be visible if compounds panel is also visible.
    /// </summary>
    [Export]
    public bool ShowAgents
    {
        get => showAgents;
        set
        {
            if (showAgents == value)
                return;

            showAgents = value;
            UpdatePanelShowAnimation();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (!ShowAgents)
            HideImmediately();
    }

    /// <summary>
    ///   Add bars to the secondary, agents holder
    /// </summary>
    public void AddAgentBar(CompoundProgressBar agentBar)
    {
        if (expandButton == null)
            throw new InvalidOperationException("Needs to be in tree first");

        agentsContainer.AddChild(agentBar);
        agentsCreatedBars.Add(agentBar);
    }

    protected override void HideImmediately()
    {
        if (!ShowPanel)
        {
            base.HideImmediately();
            currentCompoundsState = false;
            currentAgentsState = false;
        }
        else if (!ShowAgents)
        {
            agentsParentContainer.Hide();
            currentAgentsState = false;
        }
    }

    protected override void UpdatePanelState()
    {
        if (expandButton == null)
            return;

        base.UpdatePanelState();

        if (PanelCompressed)
        {
            primaryBarContainer.AddThemeConstantOverride(vSeparationReference, 20);
            primaryBarContainer.AddThemeConstantOverride(hSeparationReference, 14);

            if (primaryBars.Count < 4)
            {
                primaryBarContainer.Columns = 2;
            }
            else
            {
                primaryBarContainer.Columns = 3;
            }

            foreach (var bar in primaryBars)
            {
                bar.Compact = true;
            }

            agentsContainer.Columns = 2;

            foreach (var bar in agentsCreatedBars)
            {
                bar.Compact = true;
            }
        }
        else
        {
            primaryBarContainer.Columns = 1;
            primaryBarContainer.AddThemeConstantOverride(vSeparationReference, 5);
            primaryBarContainer.AddThemeConstantOverride(hSeparationReference, 0);

            foreach (var bar in primaryBars)
            {
                bar.Compact = false;
            }

            agentsContainer.Columns = 1;

            foreach (var bar in agentsCreatedBars)
            {
                bar.Compact = false;
            }
        }
    }

    protected override void UpdatePanelShowAnimation()
    {
        if (panelHideAnimationPlayer == null)
            return;

        if (currentAgentsState == ShowAgents && currentCompoundsState == ShowPanel)
            return;

        // Determine which of the 6 animations we should play
        // TODO: if there was a way for animation player to allow having the initial animation key frame grab the
        // previous state, that would allow cutting this down

        // Handle the more complex state first where we also need to handle agents panel
        if (currentAgentsState != ShowAgents)
        {
            if (ShowPanel == currentCompoundsState)
            {
                // Showing / hiding agents panel while other part stays visible

                if (ShowAgents)
                {
                    panelHideAnimationPlayer.Play("AddAgents");
                    currentAgentsState = true;
                }
                else
                {
                    panelHideAnimationPlayer.Play("HideAgents");
                    currentAgentsState = false;
                }
            }
            else
            {
                // Both panels move at once

                if (!ShowAgents && ShowPanel)
                {
                    panelHideAnimationPlayer.Play("ShowOnlyCompounds");
                    currentCompoundsState = true;
                    currentAgentsState = false;
                }
                else if (!ShowPanel)
                {
                    panelHideAnimationPlayer.Play("HideBoth");
                    currentCompoundsState = false;
                    currentAgentsState = false;
                }
                else
                {
                    panelHideAnimationPlayer.Play("ShowBoth");
                    currentCompoundsState = false;
                    currentAgentsState = false;
                }
            }
        }
        else if (ShowPanel)
        {
            panelHideAnimationPlayer.Play("ShowOnlyCompounds");
            currentCompoundsState = true;
            currentAgentsState = false;
        }
        else
        {
            panelHideAnimationPlayer.Play("HideOnlyCompounds");
            currentCompoundsState = false;
            currentAgentsState = false;
        }

        if (currentAgentsState != ShowAgents || currentCompoundsState != ShowPanel)
        {
            GD.PrintErr($"Panel animation states didn't result in wanted final state. Panel: " +
                $"{currentCompoundsState} != {ShowPanel} (wanted) or {currentAgentsState} != {ShowAgents} (wanted)");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            vSeparationReference.Dispose();
            hSeparationReference.Dispose();
        }

        base.Dispose(disposing);
    }
}
