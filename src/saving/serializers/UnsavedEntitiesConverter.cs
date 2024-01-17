using System;
using Newtonsoft.Json;

/// <summary>
///   Handles populating <see cref="SaveContext.UnsavedEntities"/> when saving objects of <see cref="UnsavedEntities"/>
///   type for later being ignored in <see cref="EntityReferenceConverter"/> and <see cref="EntityWorldConverter"/>.
/// </summary>
public class UnsavedEntitiesConverter : JsonConverter
{
    private static readonly Type UnsavedType = typeof(UnsavedEntities);
    private readonly SaveContext context;

    public UnsavedEntitiesConverter(SaveContext context)
    {
        this.context = context;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        // Entities that are not saved info never needs to be saved, so this can just always write an empty object
        writer.WriteStartObject();
        writer.WriteEndObject();

        // Activate the unsaved list
        ((UnsavedEntities)value).ActivateOnContext(context);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        // Read past the object
        reader.Read();

        // See WriteJson why we don't need to bother reading anything

        if (reader.TokenType != JsonToken.EndObject)
            throw new JsonException("Expected unsaved entities object to end");

        return new UnsavedEntities();
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == UnsavedType;
    }
}
