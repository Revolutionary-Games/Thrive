using Godot;

public interface IOrganelleUpgrader
{
    public void OnStartFor(OrganelleTemplate organelle);

    // TODO: allow checking for data validness / enough MP before applying the changes
    public void ApplyChanges(ICellEditorData editor);

    public Vector2 GetMinDialogSize();
}
