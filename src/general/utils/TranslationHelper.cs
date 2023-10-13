using System;
using System.Reflection;
using Godot;

/// <summary>
///   Contains helper methods for classes that want to translate some of their properties or fields
/// </summary>
public static class TranslationHelper
{
    private const BindingFlags VALID_VISIBILITY_FOR_CHECKS =
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    /// <summary>
    ///   Applies translations to fields of instance marked with TranslateFromAttribute
    /// </summary>
    /// <param name="instance">Object to process</param>
    public static void ApplyTranslations(object instance)
    {
        var type = instance.GetType();

        foreach (var field in type.GetFields(VALID_VISIBILITY_FOR_CHECKS))
        {
            var attributes = field.GetCustomAttributes(typeof(TranslateFromAttribute), true);

            if (attributes.Length < 1)
                continue;

            field.SetValue(instance, GetTranslatedText(type, instance, attributes));
        }

        foreach (var property in type.GetProperties(VALID_VISIBILITY_FOR_CHECKS))
        {
            var attributes = property.GetCustomAttributes(typeof(TranslateFromAttribute), true);

            if (attributes.Length < 1)
                continue;

            property.SetValue(instance, GetTranslatedText(type, instance, attributes));
        }
    }

    /// <summary>
    ///   Copies the translate template, that is assumed to be currently in any property marked as translation target
    ///   with TranslateFromAttribute, to the field it is translated from. This kinda does the opposite as
    ///   ApplyTranslations, and in many cases needs to be called once before translations can be performed
    /// </summary>
    /// <param name="instance">The object instance to process</param>
    public static void CopyTranslateTemplatesToTranslateSource(object instance)
    {
        var type = instance.GetType();

        foreach (var field in type.GetFields(VALID_VISIBILITY_FOR_CHECKS))
        {
            var attributes = field.GetCustomAttributes(typeof(TranslateFromAttribute), true);

            if (attributes.Length < 1)
                continue;

            var data = (TranslateFromAttribute)attributes[0];

            SetValue((string)field.GetValue(instance), type, instance, data);
        }

        foreach (var property in type.GetProperties(VALID_VISIBILITY_FOR_CHECKS))
        {
            var attributes = property.GetCustomAttributes(typeof(TranslateFromAttribute), true);

            if (attributes.Length < 1)
                continue;

            var data = (TranslateFromAttribute)attributes[0];

            SetValue((string)property.GetValue(instance), type, instance, data);
        }
    }

    public static string TranslateFeatureFlag(bool enabled)
    {
        return enabled ?
            TranslationServer.Translate("FEATURE_ENABLED") :
            TranslationServer.Translate("FEATURE_DISABLED");
    }

    public static string TranslateBoolean(bool value)
    {
        return value ? TranslationServer.Translate("YES") : TranslationServer.Translate("NO");
    }

    private static string GetTranslatedText(Type type, object instance, object[] translateAttributes)
    {
        var data = (TranslateFromAttribute)translateAttributes[0];

        var source = GetValue(type, instance, data);

        if (string.IsNullOrWhiteSpace(source))
            throw new Exception("Text to translate from is empty");

        return TranslationServer.Translate(source);
    }

    private static string? GetValue(Type type, object instance, TranslateFromAttribute data)
    {
        // Try field first
        var field = GetTargetField(type, data);

        if (field != null)
        {
            return (string?)field.GetValue(instance);
        }

        // Then property
        var property = GetTargetProperty(type, data);

        if (property == null)
            throw new NullReferenceException($"could not find translate source field or property: ${data.SourceField}");

        return (string?)property.GetValue(instance);
    }

    private static void SetValue(string value, Type type, object instance, TranslateFromAttribute data)
    {
        // Try field first
        var field = GetTargetField(type, data);

        if (field != null)
        {
            field.SetValue(instance, value);
            return;
        }

        // Then property
        var property = GetTargetProperty(type, data);

        if (property == null)
            throw new NullReferenceException($"could not find translate target field or property: ${data.SourceField}");

        property.SetValue(instance, value);
    }

    private static FieldInfo? GetTargetField(Type type, TranslateFromAttribute data)
    {
        return type.GetField(data.SourceField, VALID_VISIBILITY_FOR_CHECKS);
    }

    private static PropertyInfo? GetTargetProperty(Type type, TranslateFromAttribute data)
    {
        return type.GetProperty(data.SourceField, VALID_VISIBILITY_FOR_CHECKS);
    }
}

/// <summary>
///   Specifies from which field this property or field is translated from. Used by TranslationHelper
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TranslateFromAttribute : Attribute
{
    public TranslateFromAttribute(string sourceField)
    {
        SourceField = sourceField;
    }

    public string SourceField { get; }
}
