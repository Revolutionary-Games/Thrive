using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Serializer for PatchMap
/// </summary>
public class PatchMapConverter : SingleTypeConverter<PatchMap>
{
    private const string ADJACENT_KEY = "AdjacentPatches";

    public PatchMapConverter(ISaveContext context) : base(context)
    {
    }

    protected override bool WriteDerivedJson(JsonWriter writer, PatchMap value, JsonSerializer serializer)
    {
        return false;
    }

    protected override void WriteMember(string name, object memberValue, Type memberType, JsonWriter writer,
        JsonSerializer serializer)
    {
        if (name == nameof(PatchMap.CurrentPatch))
        {
            writer.WritePropertyName(name);

            if (memberValue == null)
            {
                serializer.Serialize(writer, null);
            }
            else
            {
                serializer.Serialize(writer, ((Patch)memberValue).ID);
            }
        }
        else
        {
            base.WriteMember(name, memberValue, memberType, writer, serializer);
        }
    }

    protected override object ReadMember(string name, Type memberType, JObject item, object instance, JsonReader reader,
        JsonSerializer serializer)
    {
        if (name == nameof(PatchMap.CurrentPatch))
        {
            var value = item[name];

            if (value == null)
                return null;

            var casted = (PatchMap)instance;
            casted.GetUnAppliedData().CurrentPatch = value.ToObject<int?>(serializer);

            return null;
        }
        else
        {
            return base.ReadMember(name, memberType, item, instance, reader, serializer);
        }
    }

    protected override void WriteCustomExtraFields(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WritePropertyName(ADJACENT_KEY);

        // Serialize the patch links
        var temp = new HashSet<(int From, int To)>();

        foreach (var entry in ((PatchMap)value).Patches)
        {
            foreach (var link in entry.Value.Adjacent)
            {
                temp.Add((entry.Value.ID, link.ID));
            }
        }

        serializer.Serialize(writer, temp);
    }

    protected override void ReadCustomExtraFields(JObject item, object instance, JsonReader reader, Type objectType,
        object existingValue, JsonSerializer serializer)
    {
        var value = item[ADJACENT_KEY];

        if (value == null)
            return;

        var casted = (PatchMap)instance;

        casted.GetUnAppliedData().AdjacentPatches = value.ToObject<HashSet<(int From, int To)>>(serializer);
    }
}
