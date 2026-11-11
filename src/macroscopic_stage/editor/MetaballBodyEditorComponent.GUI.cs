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

        toleranceWarningsPanel.GenerateToleranceProblemList(tolerances,
            MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(tolerances),
            out bool areToleranceWarnings);

        toleranceWarningsPanel.Visible = areToleranceWarnings;

        if (areToleranceWarnings)
        {
            // Need to make sure the player is not locked out of the tab to address these
            tolerancesTabButton.Visible = true;
        }
    }

    private void OnTolerancesEditorChangedData()
    {
        OnTolerancesChanged(tolerancesEditor.CurrentTolerances);
    }

    private void ChangeCurrentTransformTool(int to)
    {
        if (MovingPlacedMetaball != null)
        {
            return;
        }

        var newTool = (TransformTool)to;

        if (newTool == SelectedTransformTool)
        {
            // Toggle the tool off
            SelectedTransformTool = TransformTool.None;
            return;
        }

        SelectedTransformTool = newTool;
    }
}
