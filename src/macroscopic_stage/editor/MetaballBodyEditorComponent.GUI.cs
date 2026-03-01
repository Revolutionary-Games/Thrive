using System.Collections.Generic;
using Godot;

/// <summary>
///   GUI-specific parts of this class
/// </summary>
public partial class MetaballBodyEditorComponent
{
    private readonly List<Label> activeToleranceWarnings = new();

    private int usedToleranceWarnings;

    private void CalculateAndDisplayToleranceWarnings()
    {
        // We exclude bonuses here so that the warnings display doesn't have a partial line about a debuff and then
        // inexplicably also a bonus percentage as that would be very confusing to see.
        var tolerances = CalculateRawTolerances(true);

        MicrobeEnvironmentalToleranceCalculations.ManageToleranceProblemListGUI(ref usedToleranceWarnings,
            activeToleranceWarnings, tolerances,
            MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(tolerances), toleranceWarningContainer,
            toleranceWarningsFont, MaxToleranceWarnings);

        if (usedToleranceWarnings > 0)
        {
            tolerancesTabButton.Visible = true;
        }
    }

    private void OnTolerancesEditorChangedData()
    {
        OnTolerancesChanged(tolerancesEditor.CurrentTolerances);
    }
}
