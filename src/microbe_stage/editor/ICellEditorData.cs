using System.Collections.Generic;

/// <summary>
///   Data needed by the cell editor to function / apply the modifications
/// </summary>
public interface ICellEditorData : IHexEditor, IEditorWithPatches, IEditorWithActions
{
    /// <summary>
    ///   Properties of the edited cell. Note that organelles aren't updated while edit is in progress, for that see
    ///   <see cref="EditedCellOrganelles"/>
    /// </summary>
    public ICellDefinition? EditedCellProperties { get; }

    /// <summary>
    ///   Access to the latest edited organelle data
    /// </summary>
    public IReadOnlyList<OrganelleTemplate>? EditedCellOrganelles { get; }
}
