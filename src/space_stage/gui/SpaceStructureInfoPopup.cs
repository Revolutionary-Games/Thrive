using Godot;

/// <summary>
///   Info and possible actions on a space structure
/// </summary>
public class SpaceStructureInfoPopup : CustomDialog
{
    [Export]
    public NodePath? StructureStatusTextLabelPath;

#pragma warning disable CA2213
    private Label structureStatusTextLabel = null!;
#pragma warning restore CA2213

    private PlacedSpaceStructure? managedStructure;

    private float elapsed = 1;

    public override void _Ready()
    {
        base._Ready();

        structureStatusTextLabel = GetNode<Label>(StructureStatusTextLabelPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!Visible || managedStructure == null)
            return;

        elapsed += delta;

        if (elapsed > Constants.SPACE_STAGE_STRUCTURE_PROCESS_INTERVAL)
        {
            elapsed = 0;

            UpdateInfo();
        }
    }

    /// <summary>
    ///   Opens this screen for a structure
    /// </summary>
    public void ShowForStructure(PlacedSpaceStructure structure)
    {
        if (Visible)
        {
            Close();
        }

        managedStructure = structure;
        elapsed = 1;

        UpdateInfo();
        Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StructureStatusTextLabelPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateInfo()
    {
        WindowTitle = managedStructure!.ReadableName;
        structureStatusTextLabel.Text = managedStructure.StructureExtraDescription;

        // TODO: show buttons for the possible actions
    }
}
