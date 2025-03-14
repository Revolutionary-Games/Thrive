namespace Tutorial;

using System;

/// <summary>
///   Helper for making tutorials that count how many times the cell editor has been entered
/// </summary>
public abstract class CellEditorEntryCountingTutorial : EditorEntryCountingTutorial
{
    private readonly string cellEditorTab = EditorTab.CellEditor.ToString();

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
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
}
