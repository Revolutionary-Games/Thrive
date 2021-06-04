using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

/// <summary>
///   Saves an object as a JSON string in base64 encoded form that BinaryFormatter creates
/// </summary>
public class Base64BinaryConverter : BaseThriveConverter
{
    private readonly BinaryFormatter formatter;

    public Base64BinaryConverter(ISaveContext context) : base(context)
    {
        formatter = new BinaryFormatter { Binder = new SerializationBinder() };
    }

    public override bool CanConvert(Type objectType)
    {
        // Apparently there is no good way to determine if binary serializer supports some type or not
        return true;
    }

    protected override (object read, bool performed) ReadCustomJson(JsonReader reader, Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        var encoded = serializer.Deserialize<string>(reader);

        if (string.IsNullOrEmpty(encoded))
            return (null, true);

        using var dataReader = new MemoryStream(Convert.FromBase64String(encoded));
        return (formatter.Deserialize(dataReader), true);
    }

    protected override bool WriteCustomJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        // Would be nice to be able to know beforehand how many bytes we need
        using var dataWriter = new MemoryStream();
        formatter.Serialize(dataWriter, value);

        serializer.Serialize(writer, Convert.ToBase64String(dataWriter.GetBuffer()));

        return true;
    }
}
