/// <summary>
///   GUI-specific parts of this class
/// </summary>
public partial class MetaballBodyEditorComponent
{
    public void OnSkinTypeSelected(int newValue)
    {
        SkinType = (CreatureSkinType)newValue;
    }

    public void UpdateSkinTypeButton()
    {
        creatureSkinTypeChoiceButton.Selected = (int)SkinType;
    }

    private void CalculateAndDisplayToleranceWarnings()
    {
        // We exclude bonuses here so that the warnings display doesn't have a partial line about a debuff and then
        // inexplicably also a bonus percentage as that would be very confusing to see.
        var tolerances = CalculateRawTolerances(true);

        MicrobeEnvironmentalToleranceCalculations.GenerateToleranceProblemList(tolerances,
            MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(tolerances), toleranceWarningsPanel,
            out bool areToleranceWarnings);

        toleranceWarningsPanel.Visible = areToleranceWarnings;
    }

    private void OnTolerancesEditorChangedData()
    {
        OnTolerancesChanged(tolerancesEditor.CurrentTolerances);
    }
}
