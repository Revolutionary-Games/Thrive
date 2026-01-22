using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Microbe patch map GUI
/// </summary>
[IgnoreNoMethodsTakingInput]
public partial class MicrobeEditorPatchMap : PatchMapEditorComponent<IEditorWithPatches>
{
    public const ushort SERIALIZATION_VERSION_DERIVED = 1;

    private readonly Action<Patch> micheSelectDelegate;

#pragma warning disable CA2213
    [Export]
    private PopupMicheViewer micheViewer = null!;
#pragma warning restore CA2213

    public MicrobeEditorPatchMap()
    {
        micheSelectDelegate = OnShowMiche;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION_DERIVED;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MicrobeEditorPatchMap;

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.Write(SERIALIZATION_VERSION);
        base.WritePropertiesToArchive(writer);

        // This has no current properties to save, but this is implemented here just in case there is a future need
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_DERIVED or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_DERIVED);

        base.ReadPropertiesFromArchive(reader, reader.ReadUInt16());
    }

    public void MarkDrawerDirty()
    {
        mapDrawer.MarkDirty();
    }

    public void UpdatePatchEvents()
    {
        mapDrawer.UpdatePatchEvents();
    }

    protected override void UpdateShownPatchDetails()
    {
        base.UpdateShownPatchDetails();

        var patch = mapDrawer.SelectedPatch;

        if (patch == null)
            return;

        Editor.CurrentGame.TutorialState.SendEvent(TutorialEventType.MicrobeEditorPatchSelected,
            new PatchEventArgs(patch), this);
    }

    protected override Action<Patch> GetMicheSelectionCallback()
    {
        return micheSelectDelegate;
    }

    private void OnShowMiche(Patch patch)
    {
        if (Editor.PreviousAutoEvoResults == null)
        {
            GD.PrintErr("Missing auto-evo results, can't show miches");
            return;
        }

        micheViewer.ShowMiches(patch, Editor.PreviousAutoEvoResults.GetMicheForPatch(patch),
            Editor.CurrentGame.GameWorld.WorldSettings);
    }
}
