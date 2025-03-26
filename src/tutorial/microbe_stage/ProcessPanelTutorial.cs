namespace Tutorial;

using System;
using System.Text.Json.Serialization;

/// <summary>
///   Tutorial explaining the process panel once it is opened
/// </summary>
public class ProcessPanelTutorial : TutorialPhase
{
    [JsonIgnore]
    public override string ClosedByName => "ProcessPanelTutorial";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.ProcessPanelTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.ProcessPanelOpened:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                }

                break;
            }
        }

        return false;
    }
}
