using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public class SpawnEventConverter : JsonConverter
{
    private static IFormatProvider ifp = new NumberFormatInfo();

    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        if (value is Dictionary<SpawnSystem.IVector3, SpawnSystem.SpawnEvent> dictionary)
        {
            int i = 0;
            foreach (SpawnSystem.IVector3 key in dictionary.Keys)
            {
                WriteJObject(writer, serializer, key, dictionary[key], i.ToString(ifp));
                i++;
            }
        }

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
            return null;

        Dictionary<SpawnSystem.IVector3, SpawnSystem.SpawnEvent> dictionary =
            new Dictionary<SpawnSystem.IVector3, SpawnSystem.SpawnEvent>();

        while (reader.Read())
        {
            GD.Print(reader.Value);
            if ((string)reader.Value != "X")
                continue;

            reader.Read();
            GD.Print(reader.Value);
            int x = (int)reader.Value;
            reader.Read();
            GD.Print(reader.Value);
            reader.Read();
            GD.Print(reader.Value);
            int y = (int)reader.Value;
            reader.Read();
            GD.Print(reader.Value);
            reader.Read();
            GD.Print(reader.Value);
            int z = (int)reader.Value;

            reader.Read();
            GD.Print(reader.Value);
            reader.Read();
            GD.Print(reader.Value);
            bool isSpawned = (bool)reader.Value;
            reader.Read();
            GD.Print(reader.Value);
            reader.Read();
            GD.Print(reader.Value);
            Vector3 position = (Vector3)reader.Value;
            reader.Read();
            GD.Print(reader.Value);
            reader.Read();
            GD.Print(reader.Value);
            int gX = (int)reader.Value;
            reader.Read();
            GD.Print(reader.Value);
            reader.Read();
            GD.Print(reader.Value);
            int gY = (int)reader.Value;
            reader.Read();
            GD.Print(reader.Value);
            reader.Read();
            GD.Print(reader.Value);
            int gZ = (int)reader.Value;

            SpawnSystem.IVector3 key = new SpawnSystem.IVector3(x, y, z);

            SpawnSystem.IVector3 gridPos = new SpawnSystem.IVector3(gX, gY, gZ);
            SpawnSystem.SpawnEvent spawnEvent = new SpawnSystem.SpawnEvent(position, gridPos);
            spawnEvent.IsSpawned = isSpawned;

            dictionary.Add(key, spawnEvent);
        }

        return dictionary;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(SpawnSystem.IVector3) || objectType == typeof(SpawnSystem.SpawnEvent);
    }

    private void WriteJObject(JsonWriter writer, JsonSerializer serializer, SpawnSystem.IVector3 iVector3,
        SpawnSystem.SpawnEvent spawnEvent, string property)
    {
        writer.WritePropertyName(property);
        writer.WriteStartObject();

        writer.WritePropertyName("X");
        serializer.Serialize(writer, iVector3.X);

        writer.WritePropertyName("Y");
        serializer.Serialize(writer, iVector3.Y);

        writer.WritePropertyName("Z");
        serializer.Serialize(writer, iVector3.Z);

        writer.WritePropertyName("IsSpawned");
        serializer.Serialize(writer, spawnEvent.IsSpawned);

        writer.WritePropertyName("Position");
        serializer.Serialize(writer, spawnEvent.Position);

        writer.WritePropertyName("gX");
        serializer.Serialize(writer, spawnEvent.GridPos.X);

        writer.WritePropertyName("gY");
        serializer.Serialize(writer, spawnEvent.GridPos.Y);

        writer.WritePropertyName("gZ");
        serializer.Serialize(writer, spawnEvent.GridPos.Z);

        writer.WriteEndObject();
    }
}
