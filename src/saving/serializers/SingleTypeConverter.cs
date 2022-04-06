using System;
using Newtonsoft.Json;

/// <summary>
///   Helper for making single type converters
/// </summary>
/// <typeparam name="T">The converted type</typeparam>
public class SingleTypeConverter<T> : BaseThriveConverter
{
    private static readonly Type SupportedType = typeof(T);

    protected SingleTypeConverter(ISaveContext context) : base(context)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == SupportedType;
    }

    protected override bool WriteCustomJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        return WriteDerivedJson(writer, (T)value, serializer);
    }

    protected virtual bool WriteDerivedJson(JsonWriter writer, T value, JsonSerializer serializer)
    {
        return false;
    }
}
