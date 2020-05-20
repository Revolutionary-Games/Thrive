using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Serializer for Patch
/// </summary>
public class PatchConverter : SingleTypeConverter<Patch>
{
    public PatchConverter(ISaveContext context) : base(context)
    {
    }

    protected override bool WriteDerivedJson(JsonWriter writer, Patch value, JsonSerializer serializer)
    {
        return false;
    }

    protected override void WriteMember(string name, object memberValue, Type memberType, JsonWriter writer,
        JsonSerializer serializer)
    {
        if (name == "SpeciesInPatch")
        {
            writer.WritePropertyName(name);

            // Serialize the keys as id numbers
            var temp = new Dictionary<uint, int>();

            foreach (var entry in (Dictionary<Species, int>)memberValue)
            {
                temp[entry.Key.ID] = entry.Value;
            }

            serializer.Serialize(writer, temp);
        }
        else
        {
            base.WriteMember(name, memberValue, memberType, writer, serializer);
        }
    }

    protected override object ReadMember(string name, Type memberType, JObject item, object instance, JsonReader reader,
        JsonSerializer serializer)
    {
        if (name == "SpeciesInPatch")
        {
            var value = item[name];

            if (value == null)
                return null;

            var casted = (Patch)instance;

            casted.UnAppliedSaveData = new Patch.LoadingData
            {
                Populations = value.ToObject<Dictionary<uint, int>>(serializer),
            };

            // Return an empty value for filling later
            return Activator.CreateInstance(memberType);
        }
        else
        {
            return base.ReadMember(name, memberType, item, instance, reader, serializer);
        }
    }
}
