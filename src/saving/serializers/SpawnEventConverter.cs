using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public class SpawnEventConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {

        writer.WriteStartObject();

        if (value is SpawnSystem.IVector3 iVector3)
        {
            writer.WritePropertyName("X");
            serializer.Serialize(writer, iVector3.X);

            writer.WritePropertyName("Y");
            serializer.Serialize(writer, iVector3.Y);

            writer.WritePropertyName("Z");
            serializer.Serialize(writer, iVector3.Z);
        }
        else if (value is SpawnSystem.SpawnEvent spawnEvent)
        {
            writer.WritePropertyName("IsSpawned");
            serializer.Serialize(writer, spawnEvent.IsSpawned);

            writer.WritePropertyName("Position");
            serializer.Serialize(writer, spawnEvent.Position);

            writer.WritePropertyName("GridPos");
            serializer.Serialize(writer, spawnEvent.GridPos);
        }

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
            return null;

        JObject item = JObject.Load(reader);

        if (item.ContainsKey("X"))
        {
            int x = item["X"].Value<int>();
            int y = item["Y"].Value<int>();
            int z = item["Z"].Value<int>();
            return new SpawnSystem.IVector3(x, y, z);
        }
        else if (item.ContainsKey("IsSpawned"))
        {
            bool isSpawned = item["IsSpawned"].Value<bool>();
            Vector3 position = item["Position"].Value<Vector3>();
            SpawnSystem.IVector3 gridPos = item["GridPos"].Value<SpawnSystem.IVector3>();

            SpawnSystem.SpawnEvent spawnEvent = new SpawnSystem.SpawnEvent(position, gridPos);
            spawnEvent.IsSpawned = isSpawned;

            return spawnEvent;
        }
        else
            return null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(SpawnSystem.IVector3) || objectType == typeof(SpawnSystem.SpawnEvent);
    }
}
