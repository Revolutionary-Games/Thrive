namespace Tutorial;

using System;
using Newtonsoft.Json;

/// <summary>
///   Prompts the player to eventually place a nucleus for further progression
/// </summary>
public class NucleusTutorial : TutorialPhase
{
    private const int TriggersOnNthEditorSession = 11;

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
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                var eventArgs = (OrganellePlacedEventArgs)args;
                var isNucleus = eventArgs.Definition.Name == "Nucleus";

                if (isNucleus)
                {
                    Inhibit();
                }

                break;
            }

            case TutorialEventType.EnteredMicrobeEditor:
            {
                ++EditorEntryCount;

                CanTrigger = EditorEntryCount >= TriggersOnNthEditorSession;

                break;
            }

            case TutorialEventType.MicrobeEditorTabChanged:
            {
                if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == cellEditorTab)
                {
                    Show();
                }

                if (ShownCurrently && ((StringEventArgs)args).Data != cellEditorTab)
                {
                    Hide();
                }

                break;
            }
        }

        return false;
    }
}
