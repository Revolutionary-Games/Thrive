namespace Tutorial;

using System;
using Newtonsoft.Json;

/// <summary>
///   Second tutorial for the patch map screen
/// </summary>
public class MigrationTutorial : TutorialPhase
{
    private readonly string patchMapTab = EditorTab.PatchMap.ToString();
    private readonly string cellEditorTab = EditorTab.CellEditor.ToString();

    public MigrationTutorial()
    {
        CanTrigger = false;
    }

    [JsonIgnore]
    public override string ClosedByName => "MigrationTutorial";

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.MigrationTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (!HasBeenShown && CanTrigger && tab == patchMapTab && !overallState.TutorialActive())
                {
                    Show();
                }

                if (ShownCurrently && tab == cellEditorTab)
                {
                    Hide();
                }

                break;
            }

            case TutorialEventType.EnteredMicrobeEditor:
            {
                CanTrigger = overallState.PatchMap.Complete;
                break;
            }

            case TutorialEventType.EditorMigrationCreated:
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
