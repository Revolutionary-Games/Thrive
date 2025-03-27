namespace Tutorial;

using System;

/// <summary>
///   Explains the digestion stat once something changes them
/// </summary>
public class DigestionStatTutorial : TutorialPhase
{
    private readonly Enzyme rusticyanin = SimulationParameters.Instance.GetEnzyme("rusticyanin");

    public override string ClosedByName => "DigestionStatTutorial";

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.DigestionStatTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                if (args is not OrganellePlacedEventArgs organellePlacedEventArgs)
                    break;

                var digestionChanging = false;
                foreach (var enzyme in organellePlacedEventArgs.Definition.Enzymes.Keys)
                {
                    if (enzyme.InternalName == rusticyanin.InternalName)
                        continue;

                    digestionChanging = true;
                    break;
                }

                if (!HasBeenShown && CanTrigger && digestionChanging && !overallState.TutorialActive())
                {
                    Show();
                }

                break;
            }
        }

        return false;
    }
}
