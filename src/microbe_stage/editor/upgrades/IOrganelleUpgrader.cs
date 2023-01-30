using Godot;

public interface IOrganelleUpgrader
{
    public void OnStartFor(OrganelleTemplate organelle);

    /// <summary>
    ///   Called by the upgrade GUI when the changes should be applied
    /// </summary>
    /// <param name="editor">The editor instance to apply the changes to</param>
    /// <returns>
    ///   True should be returned on success, false if incorrect data is selected or, for example,
    ///   the player is out of MP
    /// </returns>
    public bool ApplyChanges(ICellEditorData editor);

    public Vector2 GetMinDialogSize();
}
