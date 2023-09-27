using Godot;

[JSONAlwaysDynamicType]
public class ColourActionData : EditorCombinableActionData<CellType>
{
    public Color NewColour;
    public Color PreviousColour;

    public ColourActionData(Color newColour, Color previousColour)
    {
        NewColour = newColour;
        PreviousColour = previousColour;
    }

    public override bool WantsMergeWith(CombinableActionData other)
    {
        return other is ColourActionData;
    }

    protected override int CalculateCostInternal()
    {
        // Changing membrane colour has no cost
        return 0;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        if (other is ColourActionData colourChangeActionData)
        {
            // If the value has been changed back to a previous value
            if (NewColour.IsEqualApprox(colourChangeActionData.PreviousColour) &&
                colourChangeActionData.NewColour.IsEqualApprox(PreviousColour))
            {
                return ActionInterferenceMode.CancelsOut;
            }

            // If the value has been changed twice
            if (NewColour.IsEqualApprox(colourChangeActionData.PreviousColour) ||
                colourChangeActionData.NewColour.IsEqualApprox(PreviousColour))
            {
                return ActionInterferenceMode.Combinable;
            }
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        var colourChangeActionData = (ColourActionData)other;

        if (PreviousColour.IsEqualApprox(colourChangeActionData.NewColour))
            return new ColourActionData(NewColour, colourChangeActionData.PreviousColour);

        return new ColourActionData(colourChangeActionData.NewColour, PreviousColour);
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var colourChangeActionData = (ColourActionData)other;

        if (PreviousColour.IsEqualApprox(colourChangeActionData.NewColour))
        {
            // Handle cancels out
            if (NewColour.IsEqualApprox(colourChangeActionData.PreviousColour))
            {
                NewColour = colourChangeActionData.NewColour;
                return;
            }

            PreviousColour = colourChangeActionData.PreviousColour;
            return;
        }

        NewColour = colourChangeActionData.NewColour;
    }
}
