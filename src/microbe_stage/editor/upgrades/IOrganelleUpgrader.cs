public interface IOrganelleUpgrader
{
    void OnStartFor(OrganelleTemplate organelle, IOrganelleUpgradeDialog dialog);

    // TODO: allow checking for data validness / enough MP before applying the changes
    void ApplyChanges(ICellEditorData editor);
}
