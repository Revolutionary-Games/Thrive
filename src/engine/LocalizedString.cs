﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using SharedBase.Archive;

/// <summary>
///   String that can be localized on demand for different locales.
///   This class caches the translationKey.
///   ToString returns the translated text for the current locale.
/// </summary>
/// <remarks>
///   <para>
///     This class can be used on its own but was designed for the use within LocalizedStringBuilder.
///   </para>
/// </remarks>
public class LocalizedString : IFormattable, IEquatable<LocalizedString>, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string translationKey;

    private readonly object[]? formatStringArgs;

    public LocalizedString(string translationKey)
        : this(translationKey, null)
    {
    }

    public LocalizedString(string translationKey, params object[]? formatStringArgs)
    {
        this.translationKey = translationKey;

        if (formatStringArgs is { Length: > 0 })
            this.formatStringArgs = formatStringArgs;
    }

    /// <summary>
    ///   Access to the translation key to be able to inspect what this string is based on
    /// </summary>
    public string TranslationKey => translationKey;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.LocalizedString;

    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.LocalizedString)
            throw new NotSupportedException();

        writer.WriteObject((LocalizedString)obj);
    }

    public static LocalizedString ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new LocalizedString(reader.ReadString() ?? throw new NullArchiveObjectException(),
            reader.ReadObjectOrNull<object[]>());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(translationKey);

        if (formatStringArgs != null)
        {
            writer.WriteObject(formatStringArgs);
        }
        else
        {
            writer.WriteNullObject();
        }
    }

    /// <summary>
    ///   Override current format arguments (reusing the existing args array if possible). Note that only the first
    ///   argument is updated and others are left as-is
    /// </summary>
    /// <param name="arg1">First argument to set</param>
    /// <typeparam name="T">Type of the argument</typeparam>
    /// <exception cref="InvalidOperationException">
    ///   If there isn't space for at least one format arg in the args array to put the new argument into
    /// </exception>
    /// <remarks>
    ///   <para>
    ///     I'm not fully sure if this is a good optimization to do or if this is just a pointless complication.
    ///     -hhyyrylainen
    ///   </para>
    /// </remarks>
    public void UpdateFormatArgs<T>(T arg1)
        where T : notnull
    {
        if (formatStringArgs is not { Length: > 0 })
            throw new ArgumentException("This localized string doesn't have argument space allocated for 1 argument");

        formatStringArgs[0] = arg1;
    }

    /// <summary>
    ///   Variant for more arguments. See the documentation for <see cref="UpdateFormatArgs{T}"/>.
    /// </summary>
    public void UpdateFormatArgs<T, T2>(T arg1, T2 arg2)
        where T : notnull
        where T2 : notnull
    {
        if (formatStringArgs is not { Length: > 1 })
            throw new ArgumentException("This localized string doesn't have argument space allocated for 2 arguments");

        formatStringArgs[0] = arg1;
        formatStringArgs[1] = arg2;
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

    public override string ToString()
    {
        return ToString(null, null);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (formatStringArgs == null || formatStringArgs.Length == 0)
        {
            return format ?? Localization.Translate(translationKey);
        }

        try
        {
            return string.Format(formatProvider ?? CultureInfo.CurrentCulture,
                format ?? Localization.Translate(translationKey), formatStringArgs);
        }
        catch (FormatException e)
        {
            GD.PrintErr("Invalid translation format in string ", translationKey, " for current language, exception: ",
                e);
            return Localization.Translate(translationKey);
        }
    }
}
