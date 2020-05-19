using System;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Helper for making single type converters
/// </summary>
/// <typeparam name="T">The converted type</typeparam>
public abstract class SingleTypeConverter<T> : BaseThriveConverter
{
    protected SingleTypeConverter(SimulationParameters simulation) : base(simulation)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(T);
    }

    protected override bool WriteCustomJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        return WriteDerivedJson(writer, (T)value, serializer);
    }

    protected abstract bool WriteDerivedJson(JsonWriter writer, T value, JsonSerializer serializer);
}
