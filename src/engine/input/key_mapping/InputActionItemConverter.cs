using System;
using Newtonsoft.Json;

public class InputActionItemConverter : JsonConverter<InputActionItem>
{
    public override void WriteJson(JsonWriter writer, InputActionItem value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override InputActionItem ReadJson(JsonReader reader, Type objectType, InputActionItem existingValue, bool hasExistingValue,
                                             JsonSerializer serializer)
    {
        var res = (InputActionItem)InputGroupList.InputActionItemScene.Instance();
        serializer.Populate(reader, res);
        return res;
    }
}
