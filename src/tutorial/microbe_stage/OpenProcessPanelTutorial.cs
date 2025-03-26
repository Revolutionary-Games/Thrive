namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Prompts the player to open the process panel
/// </summary>
public class OpenProcessPanelTutorial : SwimmingAroundCountingTutorial
{
    public const string TUTORIAL_NAME = "OpenProcessPanelTutorial";

    public override string ClosedByName => TUTORIAL_NAME;

    [JsonIgnore]
    public Control? ProcessPanelButtonControl { get; set; }

    protected override int TriggersOnNthSwimmingSession => 3;

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
                    Inhibit();
                }

                break;
            }
        }

        return false;
    }
}
