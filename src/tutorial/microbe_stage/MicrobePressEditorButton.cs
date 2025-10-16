namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Tells the player to press the editor button if it has been enabled too long
/// </summary>
public class MicrobePressEditorButton : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "MicrobeEditorPress";

    public Control? PressEditorButtonControl { get; set; }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobePressEditorButton;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        if (gui.PressEditorButtonHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.PressEditorButtonHighlight)} has not been set");

        gui.PressEditorButtonHighlight.TargetControl = ShownCurrently ? PressEditorButtonControl : null;

        gui.EditorButtonTutorialVisible = ShownCurrently;
        gui.PressEditorButtonHighlight.Visible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerDied:
            {
                if (ShownCurrently)
                {
                    Hide();
                }

                break;
            }

            case TutorialEventType.MicrobePlayerReadyToEdit:
            {
                if (!Complete && !ProcessWhileHidden)
                {
                    ProcessWhileHidden = true;
                    Time = 0;
                }

                break;
            }

            case TutorialEventType.EnteredMicrobeEditor:
                Inhibit();
                break;
        }

        return false;
    }

    public override void Hide()
    {
        base.Hide();
        ProcessWhileHidden = false;
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        // PressEditorButtonControl is intentionally not serialized
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        base.ReadPropertiesFromArchive(reader, 1);
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        if (Time > Constants.MICROBE_EDITOR_BUTTON_TUTORIAL_DELAY && !HasBeenShown &&
            !overallState.TutorialActive())
        {
            Show();
        }
    }
}
