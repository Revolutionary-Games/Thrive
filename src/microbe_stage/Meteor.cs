using System;
using System.Collections.Generic;
using ThriveScriptsShared;

/// <summary>
///   Contains definitions of meteors for meteor impact event
/// </summary>
public class Meteor : IRegistryType
{
    public WorldEffectVisuals.WorldEffectTypes VisualEffect;

    public List<string> Chunks = new();

    public Dictionary<Compound, float> Compounds = new();

    public double Probability;

    public string InternalName { get; set; } = null!;

    public static void CheckAllMeteors(List<Meteor> meteors)
    {
        double totalSum = 0;
        foreach (Meteor meteor in meteors)
        {
            totalSum += meteor.Probability;
        }

        if (Math.Abs(totalSum - 1.0) > 1e-6)
        {
            throw new InvalidRegistryDataException($"Meteors probability sum mismatch: {totalSum}. Expected: 1.0");
        }
    }

    public void Check(string name)
    {
        if (Probability < 0)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Invalid probability value for meteor: " + InternalName);
        }
    }

    public void ApplyTranslations()
    {
    }

    public override string ToString()
    {
        return InternalName;
    }
}
