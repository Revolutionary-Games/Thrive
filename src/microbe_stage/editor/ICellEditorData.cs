using System.Collections.Generic;
using UnlockConstraints;

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

    public WorldAndPlayerDataSource UnlocksDataSource { get; }

    /// <summary>
    ///   Calculates the plain tolerance result with the current organelles and edited tolerances.
    /// </summary>
    /// <param name="excludePositiveBuffs">
    ///   If set to true, perfect adaptation bonuses will be excluded from the result. This is used to display
    ///   tolerance-related debuffs in a more sensible way where partial bonuses are not allowed to leak into the
    ///   results.
    /// </param>
    /// <returns>The calculated tolerance values</returns>
    public ToleranceResult CalculateRawTolerances(bool excludePositiveBuffs = false);

    /// <summary>
    ///   Trigger a tolerances change and notify all editor components. This method exists so that a component that
    ///   causes a tolerance change can notify all other components that might need to know.
    /// </summary>
    /// <param name="newTolerances">The new tolerance values that were just updated</param>
    public void OnTolerancesChanged(EnvironmentalTolerances newTolerances);
}
