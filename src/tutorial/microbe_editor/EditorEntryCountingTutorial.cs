namespace Tutorial;

using System;
using Newtonsoft.Json;

/// <summary>
///   Helper for making tutorials that count how many times the cell editor has been entered
/// </summary>
public abstract class EditorEntryCountingTutorial : TutorialPhase
{
    private readonly string cellEditorTab = EditorTab.CellEditor.ToString();

    [JsonProperty]
    public int NumberOfEditorEntries { get; set; }

    protected abstract int TriggersOnNthEditorSession { get; }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (CheckEventEditorEntryEvent(eventType))
            return false;

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorTabChanged:
            {
                if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == cellEditorTab &&
                    !overallState.TutorialActive())
                {
                    Show();
                }

                break;
            }
        }

        return false;
    }

    protected bool CheckEventEditorEntryEvent(TutorialEventType eventType)
    {
        if (eventType != TutorialEventType.EnteredMicrobeEditor)
            return false;

        if (!HasBeenShown)
        {
            ++NumberOfEditorEntries;
            CanTrigger = NumberOfEditorEntries >= TriggersOnNthEditorSession;
        }

        return true;
    }
}
