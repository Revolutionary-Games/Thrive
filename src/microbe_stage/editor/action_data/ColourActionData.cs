using System.Collections.Generic;
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

    protected override double CalculateBaseCostInternal()
    {
        // Changing membrane colour has no cost
        return 0;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        // No cost adjustment as this is free
        return (CalculateBaseCostInternal(), 0);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return other is ColourActionData;
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
