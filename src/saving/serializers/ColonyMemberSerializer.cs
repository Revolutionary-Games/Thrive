using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ColonyMemberSerializer : JsonConverter<ColonyMember>
{
    public override ColonyMember ReadJson(JsonReader reader, Type objectType, ColonyMember existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.Value == null)
            return null;

        var result = new ColonyMember();
        serializer.Populate(reader, result);
        foreach (var colonyMember in result.BindingTo)
        {
            colonyMember.Master = result;
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, ColonyMember value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}
