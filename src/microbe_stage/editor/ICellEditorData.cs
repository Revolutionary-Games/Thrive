using System.Collections.Generic;
using UnlockConstraints;

/// <summary>
///   Data needed by the cell editor to function / apply the modifications
/// </summary>
public interface ICellEditorData : IHexEditor, IEditorWithPatches, IEditorWithActions
{
    /// <summary>
    ///   Properties of the edited cell. Note that organelles aren't updated while an edit is in progress, for that see
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

    /// <summary>
    ///   Needs to calculate the optimal tolerances for the currently selected patch. Required by the tolerance editor
    ///   component to function. This method throws if called before the editor is fully initialized.
    /// </summary>
    /// <returns>Calculated tolerances for the currently selected patch</returns>
    public EnvironmentalTolerances GetOptimalTolerancesForCurrentPatch();

    /// <summary>
    ///   Get current tolerance status with the edits and current patch. Needed for tolerance GUI.
    /// </summary>
    /// <param name="calculationTolerances">Tolerances that are used in the calculation as a base</param>
    /// <returns>Just the normal tolerance result</returns>
    public ToleranceResult CalculateCurrentTolerances(EnvironmentalTolerances calculationTolerances);

    /// <summary>
    ///   Generate a breakdown for the tolerance GUI to show to the player
    /// </summary>
    /// <param name="toleranceCategory">Category of tolerance values</param>
    /// <param name="result">Results on what affect this are placed here</param>
    public void GetCurrentToleranceSummaryByElement(ToleranceModifier toleranceCategory,
        Dictionary<IPlayerReadableName, float> result);

    /// <summary>
    ///   Get the tolerance effects from organelles (or equivalents in a later editor)
    /// </summary>
    /// <param name="modifiedTolerances">Tolerances to put the modifications in</param>
    public void CalculateBodyEffectOnTolerances(
        ref MicrobeEnvironmentalToleranceCalculations.ToleranceValues modifiedTolerances);
}
