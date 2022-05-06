public interface IOrganelleUpgrader
{
    void OnStartFor(OrganelleTemplate organelle);

    // TODO: allow checking for data validness / enough MP before applying the changes
    void ApplyChanges(ICellEditorData editor);
}
