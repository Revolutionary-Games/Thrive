using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

public class SpawnItemConverter : JsonConverter
{
    public override bool CanRead => true;
    public override bool CanWrite => false;

    static JsonSerializerSettings SpecifiedSubclassConversion =
        new JsonSerializerSettings() { ContractResolver = new BaseSpecifiedConcreteClassConverter() };

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        switch (jo["spawnType"].Value<string>())
        {
            case CloudItem.NAME:
                return JsonConvert.DeserializeObject<CloudItem>(jo.ToString(), SpecifiedSubclassConversion);

            case ChunkItem.NAME:
                return JsonConvert.DeserializeObject<ChunkItem>(jo.ToString(), SpecifiedSubclassConversion);

            case MicrobeItem.NAME:
                return JsonConvert.DeserializeObject<MicrobeItem>(jo.ToString(), SpecifiedSubclassConversion);
        }
        return null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(SpawnItem);
    }
}

public class BaseSpecifiedConcreteClassConverter : DefaultContractResolver
{
    protected override JsonConverter ResolveContractConverter(Type objectType)
    {
        if (typeof(SpawnItem).IsAssignableFrom(objectType) && !objectType.IsAbstract)
            return null; // pretend TableSortRuleConvert is not specified (thus avoiding a stack overflow)
        return base.ResolveContractConverter(objectType);
    }
}
