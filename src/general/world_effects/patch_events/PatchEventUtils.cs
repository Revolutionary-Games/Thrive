using System.Collections.Generic;
using Godot;

public class PatchEventUtils
{
    public static int GetRandomElementByProbability(List<double> chances, double probability)
    {
        double cumulative = 0.0;
        for (var i = 0; i < chances.Count; i++)
        {
            cumulative += chances[i];
            if (probability <= cumulative)
                return i;
        }

        GD.PrintErr("Failed to select an element due to probability sum mismatch.");
        return 0;
    }
}
