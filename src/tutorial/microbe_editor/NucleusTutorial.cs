namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Prompts the player to eventually place a nucleus for further progression
/// </summary>
public class NucleusTutorial : EditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string cellEditorTab = nameof(EditorTab.CellEditor);

    private readonly OrganelleDefinition nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");

    private bool hasNucleus;

    public NucleusTutorial()
    {
        CanTrigger = false;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialNucleusTutorial;

    public override string ClosedByName => "NucleusTutorial";

    protected override int TriggersOnNthEditorSession => 10;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.NucleusTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return false;

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                if (args is not OrganellePlacedEventArgs eventArgs)
                {
                    GD.PrintErr("Organelle placed event has wrong argument type, this will break many tutorials!");
                    break;
                }

                var isNucleus = eventArgs.Definition.InternalName == nucleus.InternalName;

                if (isNucleus)
                {
                    if (ShownCurrently)
                    {
                        Hide();
                    }

                    hasNucleus = true;
                }

                break;
            }

            case TutorialEventType.EditorUndo:
            {
                var editorActionEventArgs = (EditorActionEventArgs)args;

                foreach (var data in editorActionEventArgs.Action.Data)
                {
                    if (data is not OrganellePlacementActionData organellePlacementData)
                        continue;

                    if (organellePlacementData.PlacedHex.Definition.InternalName == nucleus.InternalName)
                    {
                        hasNucleus = false;
                    }
                }

                break;
            }

            case TutorialEventType.EditorRedo:
            {
                var editorActionEventArgs = (EditorActionEventArgs)args;

                foreach (var data in editorActionEventArgs.Action.Data)
                {
                    if (data is not OrganellePlacementActionData organellePlacementData)
                        continue;

                    if (organellePlacementData.PlacedHex.Definition.InternalName == nucleus.InternalName)
                    {
                        hasNucleus = true;
                    }
                }

                break;
            }

            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (tab == cellEditorTab && !hasNucleus && CanTrigger)
                {
                    Show();
                }

                break;
            }
        }

        return false;
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        writer.Write(hasNucleus);
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);

        hasNucleus = reader.ReadBool();
    }
}
