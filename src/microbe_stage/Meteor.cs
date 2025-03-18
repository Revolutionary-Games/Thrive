using System.Collections.Generic;
using ThriveScriptsShared;

/// <summary>
///   Contains definitions of meteors for meteor impact event
/// </summary>
public class Meteor : IRegistryType
{
    public WorldEffectVisuals VisualEffect;

    public List<string> Chunks = new();

    public Dictionary<Compound, double> Compounds = new();

    public double Probability;

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return InternalName;
    }
}
