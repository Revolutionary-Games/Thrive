using System;

public static class ATPDisplayHelper
{
    private const float ATP_DISPLAY_FULL_FRACTION = 0.9f;
    private const float ATP_DISPLAY_MINIMUM_FULL_MARGIN = 0.1f;

    public static float CalculateDisplayedATPAmount(float atpAmount, float maxATP)
    {
        if (maxATP <= 0)
            return 0;

        atpAmount = Math.Clamp(atpAmount, 0, maxATP);

        // If the current ATP is close to full, just pretend that it is to keep the bar from flickering.
        var fullMargin = Math.Max(maxATP * (1 - ATP_DISPLAY_FULL_FRACTION), ATP_DISPLAY_MINIMUM_FULL_MARGIN);
        if (maxATP - atpAmount <= fullMargin)
            return maxATP;

        return atpAmount;
    }
}
