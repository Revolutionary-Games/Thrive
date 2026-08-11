using Godot;

public interface IOrganelleUpgrader
{
    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame, float costMultiplier);

    /// <summary>
    ///   Called by the upgrade GUI when the changes should be applied
    /// </summary>
    /// <param name="editorComponent">The editor component this action was done in</param>
    /// <param name="organelleUpgrades">Where to apply the organelle upgrade data</param>
    /// <returns>
    ///   True should be returned on success, false if incorrect data is selected or there's another condition that
    ///   should prevent the change. MP to be enough is checked after <see cref="OrganelleUpgradeActionData"/> is
    ///   created based on the modified data.
    /// </returns>
    public bool ApplyChanges(ICellEditorComponent editorComponent, OrganelleUpgrades organelleUpgrades);

    /// <summary>
    ///   Returns true when the currently selected settings match the organelle's implicit default upgrade state
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is used to avoid creating a history action when an organelle with no stored upgrade data is opened
    ///     and accepted without changing anything.
    ///   </para>
    /// </remarks>
    public bool IsCurrentStateDefault();

    public Vector2 GetMinDialogSize();
}
