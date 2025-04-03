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

    public void Check(string name)
    {
    }

    public void ApplyTranslations()
    {
    }

    public override string ToString()
    {
        return InternalName;
    }
}
