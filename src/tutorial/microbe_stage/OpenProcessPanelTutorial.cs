namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Prompts the player to open the process panel
/// </summary>
public class OpenProcessPanelTutorial : SwimmingAroundCountingTutorial
{
    public OpenProcessPanelTutorial()
    {
        // This needs to pause to ensure the player doesn't die while this overlay is open
        Pauses = true;
    }

    public override string ClosedByName => "OpenProcessPanelTutorial";

    [JsonIgnore]
    public Control? ProcessPanelButtonControl { get; set; }

    protected override int TriggersOnNthSwimmingSession => 2;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        if (gui.ProcessPanelButtonHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.ProcessPanelButtonHighlight)} has not been set");

        gui.ProcessPanelButtonHighlight.TargetControl = ShownCurrently ? ProcessPanelButtonControl : null;

        gui.OpenProcessPanelTutorialVisible = ShownCurrently;
        gui.ProcessPanelButtonHighlight.Visible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.ProcessPanelOpened:
            {
                if (ShownCurrently)
                {
                    Hide();
                }
                else if (!HasBeenShown)
                {
                    CanTrigger = false;

                    // Permanently inhibit only if the process panel tutorial has been shown
                    if (overallState.ProcessPanelTutorial.HasBeenShown || overallState.ProcessPanelTutorial.Complete)
                    {
                        Inhibit();
                    }
                }

                break;
            }

            case TutorialEventType.ProcessPanelProcessEnabled:
            {
                // Another check for the player going through the process panel tutorial
                if (overallState.ProcessPanelTutorial.HasBeenShown)
                {
                    Inhibit();
                }

                break;
            }
        }

        return false;
    }
}
