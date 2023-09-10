using System;
using System.IO;
using System.Numerics;
using Newtonsoft.Json;

/// <summary>
///   Binary encodes Vector4[,] type for saving space in json
/// </summary>
public class SystemVector4ArrayConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        var casted = (Vector4[,])value;

        if (casted.Rank != 2)
            throw new ArgumentException("unexpected array rank");

        int width = casted.GetLength(0);
        int height = casted.GetLength(1);

        var elementSize = 4 * sizeof(float);
        var header = sizeof(int) * 2;

        using var stream = new MemoryStream();
        stream.Capacity = elementSize * width * height + header;

        using var dataWriter = new BinaryWriter(stream);

        dataWriter.Write(width);
        dataWriter.Write(height);

        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < width; ++y)
            {
                var element = casted[x, y];

                dataWriter.Write(element.X);
                dataWriter.Write(element.Y);
                dataWriter.Write(element.Z);
                dataWriter.Write(element.W);
            }
        }

        serializer.Serialize(writer, Convert.ToBase64String(stream.GetBuffer()));
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var encoded = serializer.Deserialize<string>(reader);

        if (string.IsNullOrEmpty(encoded))
            return null;

        using var stream = new MemoryStream(Convert.FromBase64String(encoded));
        using var dataReader = new BinaryReader(stream);

        var width = dataReader.ReadInt32();
        var height = dataReader.ReadInt32();

        var result = new Vector4[width, height];

        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < width; ++y)
            {
                var elementX = dataReader.ReadSingle();
                var elementY = dataReader.ReadSingle();
                var elementZ = dataReader.ReadSingle();
                var elementW = dataReader.ReadSingle();

                result[x, y] = new Vector4(elementX, elementY, elementZ, elementW);
            }
        }

        return result;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Vector4[,]) == objectType;
    }
}
