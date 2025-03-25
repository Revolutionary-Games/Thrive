namespace Tutorial;

using System;

/// <summary>
///   Last words of the microbe editor tutorial (for the first editor cycle)
/// </summary>
public class EditorTutorialEnd : TutorialPhase
{
    public EditorTutorialEnd()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "CellEditorClosingWords";

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.CellEditorClosingWordsVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EditorRedo:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                    return true;
                }

                break;
            }
        }

        return false;
    }

    public override void Hide()
    {
        if (ShownCurrently)
        {
            OnClosed?.Invoke();
        }

        base.Hide();
    }
}
