using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Godot;
using Saving.Serializers;
using ThriveScriptsShared;

/// <summary>
///   Contains definitions of meteors for meteor impact event
/// </summary>
// [TypeConverter($"Saving.Serializers.{nameof(MeteorStringConverter)}")]
public class Meteor : IRegistryType
{
    /// <summary>
    ///   User visible pretty name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    // [TranslateFrom(nameof(untranslatedDescription))]
    // public string Description = null!;

    public List<string> Chunks { get; set; } = new();

    public Dictionary<Compound, double> Compounds { get; set; } = new();
    // public Dictionary<CompoundDefinition, float> Compounds = new();

    public double Probability { get; set; }

    // public string Icon = null!;
    //
    // [JsonIgnore]
    // public Texture2D? LoadedIcon;

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
    
    // public void Resolve(SimulationParameters parameters)
    // {
    //     LoadedIcon = GD.Load<Texture2D>(Icon);
    // }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
    
    public override string ToString()
    {
        return Name;
    }
}
