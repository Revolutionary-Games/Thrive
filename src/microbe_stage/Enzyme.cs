﻿using System.ComponentModel;

/// <summary>
///   Define enzymes in the game. Enzyme is an "upgrade" that grants specific ability to microbes.
/// </summary>
[TypeConverter(typeof(EnzymeStringConverter))]
public class Enzyme : IRegistryType
{
    /// <summary>
    ///   User visible pretty name
    /// </summary>
    [TranslateFrom("untranslatedName")]
    public string Name = null!;

    /// <summary>
    ///   What this enzyme does.
    /// </summary>
    public EnzymeProperty Property = EnzymeProperty.Hydrolytic;

#pragma warning disable 169 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169

    public enum EnzymeProperty
    {
        Hydrolytic,
        Oxidizer,
    }

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
