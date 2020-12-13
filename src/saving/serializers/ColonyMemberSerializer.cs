using System;
using Newtonsoft.Json;

public class ColonyMemberSerializer : BaseThriveConverter
{
    public ColonyMemberSerializer(ISaveContext context) : base(context)
    {
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var result = (ColonyMember)base.ReadJson(reader, objectType, existingValue, serializer);
        if (result != null)
        {
            foreach (var colonyMember in result.BindingTo)
            {
                colonyMember.Master = result;
            }
        }

        return result;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(ColonyMember).IsAssignableFrom(objectType);
    }
}
