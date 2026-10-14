namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Prompts the player to consider placing actomyosin on cells in their multicellular body plan (to move fast)
/// </summary>
public class MulticellularActomyosinTutorial : MulticellularEditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string cellEditorTab = nameof(EditorTab.CellEditor);

    private readonly OrganelleDefinition actomyosin = SimulationParameters.Instance.GetOrganelleType("actomyosin");

    private bool hasActomyosin;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMulticellularActomyosin;

    public override string ClosedByName => "MulticellularActomyosinTutorial";

    protected override int TriggersOnNthEditorSession => 4;

    public override void ApplyGUIState(MulticellularEditorTutorialGUI gui)
    {
        gui.ActomyosinTutorialVisible = ShownCurrently;
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
                    // Error is printed by many other tutorials, so this doesn't need to do it
                    break;
                }

                var isActomyosin = eventArgs.Definition.InternalName == actomyosin.InternalName;

                if (isActomyosin)
                {
                    if (ShownCurrently)
                    {
                        Hide();
                    }

                    hasActomyosin = true;
                }

                break;
            }

            case TutorialEventType.MicrobeEditorOrganelleRemoved:
            {
                // Actomyosin can be deleted like normal organelles (as it is normal), so we need to also track removal
                // events rather than just undo / redo events like the nucleus tutorial does.
                if (args is OrganelleRemovedEventArgs eventArgs &&
                    eventArgs.Definition.InternalName == actomyosin.InternalName)
                {
                    hasActomyosin = false;
                }

                break;
            }

            case TutorialEventType.EditorUndo:
            {
                var editorActionEventArgs = (EditorActionEventArgs)args;

                foreach (var data in editorActionEventArgs.Action.Data)
                {
                    if (data is OrganellePlacementActionData organellePlacementData &&
                        organellePlacementData.PlacedHex.Definition.InternalName == actomyosin.InternalName)
                    {
                        hasActomyosin = false;
                    }

                    if (data is OrganelleRemoveActionData organelleRemoveData &&
                        organelleRemoveData.RemovedHex.Definition.InternalName == actomyosin.InternalName)
                    {
                        hasActomyosin = true;
                    }
                }

                break;
            }

            case TutorialEventType.EditorRedo:
            {
                var editorActionEventArgs = (EditorActionEventArgs)args;

                foreach (var data in editorActionEventArgs.Action.Data)
                {
                    if (data is OrganellePlacementActionData organellePlacementData &&
                        organellePlacementData.PlacedHex.Definition.InternalName == actomyosin.InternalName)
                    {
                        hasActomyosin = true;
                    }

                    if (data is OrganelleRemoveActionData organelleRemoveData &&
                        organelleRemoveData.RemovedHex.Definition.InternalName == actomyosin.InternalName)
                    {
                        hasActomyosin = false;
                    }
                }

                break;
            }

            case TutorialEventType.MulticellularEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (tab == cellEditorTab && !hasActomyosin && CanTrigger && !overallState.TutorialActive())
                {
                    if (sender is MulticellularEditor editor &&
                        !actomyosin.IsIncompatibleWithMembrane(editor.EditedSpecies.ModifiableEditorCells[0].Data!
                            .MembraneType) &&
                        editor.CurrentGame.GameWorld.UnlockProgress.IsUnlocked(actomyosin, editor.UnlocksDataSource,
                            editor.CurrentGame, false))
                    {
                        Show();
                    }
                }

                break;
            }
        }

        return false;
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        writer.Write(hasActomyosin);
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);

        hasActomyosin = reader.ReadBool();
    }
}
