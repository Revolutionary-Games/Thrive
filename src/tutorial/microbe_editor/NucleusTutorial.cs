namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Prompts the player to eventually place a nucleus for further progression
/// </summary>
public class NucleusTutorial : TutorialPhase
{
    private const int TriggersOnNthEditorSession = 3;

    private readonly string cellEditorTab = EditorTab.CellEditor.ToString();

    public NucleusTutorial()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "NucleusTutorial";

    [JsonProperty]
    private int EditorEntryCount { get; set; }

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.NucleusTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EnteredMicrobeEditor:
            {
                ++EditorEntryCount;

                CanTrigger = EditorEntryCount >= TriggersOnNthEditorSession;

                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                }

                break;
            }

            case TutorialEventType.MicrobeEditorTabChanged:
            {
                if (ShownCurrently)
                {
                    Hide();
                }

                break;
            }
        }

        return false;
    }
}
