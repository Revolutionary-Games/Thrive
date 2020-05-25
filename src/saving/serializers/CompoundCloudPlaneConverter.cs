using System;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Converter for CompoundCloudPlane
/// </summary>
public class CompoundCloudPlaneConverter : BaseThriveConverter
{
    private const string DENSITY_PACKED_KEY = "EncodedDensity";
    private readonly Base64ArrayConverter densityConverter;

    public CompoundCloudPlaneConverter(ISaveContext context) : base(context)
    {
        densityConverter = new Base64ArrayConverter(context);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(CompoundCloudPlane);
    }

    protected override void WriteCustomExtraFields(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WritePropertyName(DENSITY_PACKED_KEY);

        densityConverter.WriteJson(writer, ((CompoundCloudPlane)value).Density, serializer);
    }

    protected override void ReadCustomExtraFields(JObject item, object instance, JsonReader reader, Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        base.ReadCustomExtraFields(item, instance, reader, objectType, existingValue, serializer);

        var casted = (CompoundCloudPlane)instance;

        casted.Density = (Vector4[,])densityConverter.ReadJson(item[DENSITY_PACKED_KEY].CreateReader(),
            casted.Density.GetType(), null, serializer);
    }

    protected override bool SkipMember(string name)
    {
        if (name == "Mesh")
            return true;

        return base.SkipMember(name);
    }
}
