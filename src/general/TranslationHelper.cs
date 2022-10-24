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

            var source = GetTargetField(type, data);

            source.SetValue(instance, field.GetValue(instance));
        }

        foreach (var property in type.GetProperties(VALID_VISIBILITY_FOR_CHECKS))
        {
            var attributes = property.GetCustomAttributes(typeof(TranslateFromAttribute), true);

            if (attributes.Length < 1)
                continue;

            var data = (TranslateFromAttribute)attributes[0];

            var source = GetTargetField(type, data);

            source.SetValue(instance, property.GetValue(instance));
        }
    }

    public static string TranslateBool(bool value)
    {
        return TranslationServer.Translate(value ? "TRUE" : "FALSE");
    }

    private static string GetTranslatedText(Type type, object instance, object[] attributes)
    {
        var data = (TranslateFromAttribute)attributes[0];

        var source = GetTargetField(type, data);

        return TranslationServer.Translate((string)source.GetValue(instance));
    }

    private static FieldInfo GetTargetField(Type type, TranslateFromAttribute data)
    {
        var source = type.GetField(data.SourceField, VALID_VISIBILITY_FOR_CHECKS);

        if (source == null)
            throw new NullReferenceException($"could not find translate source field: ${data.SourceField}");

        return source;
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
