﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   String that can be localized on demand for different locales.
///   This class caches the translationKey.
///   ToString returns the translated text for the current locale.
/// </summary>
/// <remarks>
///   This class can be used on its own, but was designed for the use within LocalizedStringBuilder.
/// </remarks>
[JSONDynamicTypeAllowed]
[TypeConverter(typeof(LocalizedStringTypeConverter))]
public class LocalizedString : IFormattable, IEquatable<LocalizedString>
{
    [JsonProperty]
    private readonly string translationKey;

    [JsonProperty]
    private readonly object[]? formatStringArgs;

    public LocalizedString(string translationKey)
        : this(translationKey, null)
    {
    }

    [JsonConstructor]
    public LocalizedString(string translationKey, params object[]? formatStringArgs)
    {
        this.translationKey = translationKey;
        this.formatStringArgs = formatStringArgs;
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (formatStringArgs == null || formatStringArgs.Length == 0)
        {
            return format ?? TranslationServer.Translate(translationKey);
        }

        try
        {
            return string.Format(formatProvider ?? CultureInfo.CurrentCulture,
                format ?? TranslationServer.Translate(translationKey), formatStringArgs);
        }
        catch (FormatException e)
        {
            GD.PrintErr("Invalid translation format in string ", translationKey, " for current language, exception: ",
                e);
            return TranslationServer.Translate(translationKey);
        }
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as LocalizedString);
    }

    public bool Equals(LocalizedString? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other == null)
            return false;

        if (translationKey != other.translationKey)
            return false;

        if (formatStringArgs == null)
            return other.formatStringArgs == null;

        if (other.formatStringArgs == null)
            return false;

        return formatStringArgs.SequenceEqual(other.formatStringArgs);
    }

    public override int GetHashCode()
    {
        int hashCode = 2031027761;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(translationKey);

        if (formatStringArgs != null)
        {
            hashCode = hashCode * -1521134295 + EqualityComparer<object[]>.Default.GetHashCode(formatStringArgs);
        }
        else
        {
            hashCode = hashCode * -1521134295 + 60961;
        }

        return hashCode;
    }
}
