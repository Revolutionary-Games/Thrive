using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Godot;
using Saving.Serializers;
using ThriveScriptsShared;

/// <summary>
///   Contains definitions of meteors for meteor impact event
/// </summary>
public class Meteor : IRegistryType
{
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    [TranslateFrom(nameof(untranslatedDescription))]
    public string Description = null!;

    public WorldEffectVisuals VisualEffect;

    public List<string> Chunks = new();

    public Dictionary<Compound, double> Compounds = new();

    public double Probability;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
    private string? untranslatedDescription;
#pragma warning restore 169,649

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Meteor has no name");
        }
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return Name;
    }
}
