using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SpawnItemConverter : JsonConverter
{
    private static IFormatProvider ifp = new NumberFormatInfo();
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        if (value is List<SpawnItem>)
        {
            writer.WritePropertyName("DataType");
            serializer.Serialize(writer, "List");

            List<SpawnItem> list = (List<SpawnItem>)value;
            for (int i = 0; i < list.Count; i++)
            {
                WriteSpawnItem(writer, list[i], serializer, i.ToString(ifp));
            }
        }

        if (value is Queue<SpawnItem>)
        {
            writer.WritePropertyName("DataType");
            serializer.Serialize(writer, "Queue");

            Queue<SpawnItem> queue = (Queue<SpawnItem>)value;
            for (int i = 0; i < queue.Count; i++)
            {
                WriteSpawnItem(writer, queue.Dequeue(), serializer, i.ToString(ifp));
            }
        }

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
            return null;
        reader.Read();
        reader.Read();

        if ((string)reader.Value == "List")
        {
            List<SpawnItem> spawnItems = new List<SpawnItem>();
            while (reader.Read())
            {
                if (reader.ValueType != typeof(JObject))
                    continue;
                JObject jSpawnItem = (JObject)reader.Value;
                SpawnItem spawnItem = ReadSpawnItem(jSpawnItem.CreateReader());
                if (spawnItem != null)
                {
                    spawnItems.Add(spawnItem);
                }
            }

            return spawnItems;
        }
        else if ((string)reader.Value == "Queue")
        {
            Queue<SpawnItem> spawnItems = new Queue<SpawnItem>();
            while (reader.Read())
            {
                SpawnItem spawnItem = ReadSpawnItem(reader);
                if (spawnItem != null)
                {
                    spawnItems.Enqueue(spawnItem);
                }
            }

            return spawnItems;
        }
        else
            return null;
    }

    public override bool CanConvert(Type objectType)
    {
        if (!objectType.IsGenericType)
        {
            return false;
        }

        if (objectType.GetGenericTypeDefinition() != typeof(List<>)
            || objectType.GetGenericTypeDefinition() != typeof(Queue<>))
        {
            return false;
        }

        return objectType.GetGenericArguments()[0] == typeof(SpawnItem);
    }

    private void WriteSpawnItem(JsonWriter writer, SpawnItem spawnItem, JsonSerializer serializer, string property)
    {
        writer.WritePropertyName(property);
        writer.WriteStartObject();
        writer.WritePropertyName("Position");
        serializer.Serialize(writer, spawnItem.Position);

        if (spawnItem is CloudItem)
        {
            CloudItem cloudItem = (CloudItem)spawnItem;

            writer.WritePropertyName("Compound");
            serializer.Serialize(writer, cloudItem.Compound);

            writer.WritePropertyName("Amount");
            serializer.Serialize(writer, cloudItem.Amount);
        }

        if (spawnItem is ChunkItem)
        {
            ChunkItem chunkItem = (ChunkItem)spawnItem;

            writer.WritePropertyName("ChunkType");
            serializer.Serialize(writer, chunkItem.ChunkType);
        }

        if (spawnItem is MicrobeItem)
        {
            MicrobeItem microbeItem = (MicrobeItem)spawnItem;

            writer.WritePropertyName("MicrobeSpecies");
            serializer.Serialize(writer, microbeItem.Species);
        }

        writer.WriteEndObject();
    }

    private SpawnItem ReadSpawnItem(JsonReader reader)
    {
        reader.Read();
        if ((string)reader.Value == "Position")
        {
            reader.Read();
            Vector3 position = (Vector3)reader.Value;

            reader.Read();
            string property = (string)reader.Value;
            switch (property)
            {
                case "Compound":
                    reader.Read();
                    Compound compound = (Compound)reader.Value;
                    reader.Read();
                    reader.Read();
                    float amount = (float)reader.Value;
                    CloudItem cloudItem = new CloudItem(compound, amount);
                    cloudItem.Position = position;
                    return cloudItem;

                case "ChunkType":
                    reader.Read();
                    ChunkConfiguration chunkType = (ChunkConfiguration)reader.Value;
                    ChunkItem chunkItem = new ChunkItem(chunkType);
                    chunkItem.Position = position;
                    return chunkItem;

                case "MicrobeSpecies":
                    reader.Read();
                    MicrobeSpecies microbeSpecies = (MicrobeSpecies)reader.Value;
                    MicrobeItem microbeItem = new MicrobeItem(microbeSpecies);
                    microbeItem.Position = position;
                    return microbeItem;

                default:
                    return null;
            }
        }
        else
            return null;
    }
}
