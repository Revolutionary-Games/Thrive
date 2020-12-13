using System;
using Newtonsoft.Json;

public class MicrobeConverter : BaseThriveConverter
{
    public MicrobeConverter(ISaveContext context) : base(context)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Microbe).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var result = (Microbe)base.ReadJson(reader, objectType, existingValue, serializer);
        if (result.Colony != null)
            result.Colony.Microbe = result;
        return result;
    }
}
