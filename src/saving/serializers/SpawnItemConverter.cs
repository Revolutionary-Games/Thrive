using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SpawnItemConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        SpawnItem spawnItem = (SpawnItem)value;

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

        if (spawnItem is CloudItem)
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

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
            return null;

        JObject item = JObject.Load(reader);

        if (item.ContainsKey("Compound"))
        {
            Compound compound = item["Compound"].Value<Compound>();
            float amount = item["Amount"].Value<float>();

            CloudItem cloudItem = new CloudItem(compound, amount);
            cloudItem.Position = item["Position"].Value<Vector3>();

            return cloudItem;
        }
        else if (item.ContainsKey("ChunkType"))
        {
            ChunkConfiguration chunkType = item["ChunkType"].Value<ChunkConfiguration>();

            ChunkItem chunkItem = new ChunkItem(chunkType);
            chunkItem.Position = item["Position"].Value<Vector3>();

            return chunkItem;
        }
        else if (item.ContainsKey("MicrobeSpecies"))
        {
            MicrobeSpecies species = item["MicrobeSpecies"].Value<MicrobeSpecies>();

            MicrobeItem microbeItem = new MicrobeItem(species);
            microbeItem.Position = item["Position"].Value<Vector3>();

            return microbeItem;
        }
        else
            return null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(SpawnItem);
    }
}
