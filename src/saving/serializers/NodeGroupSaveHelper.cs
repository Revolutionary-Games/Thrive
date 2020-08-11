using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
///   Common utility functions for various types to save the Godot groups they are in
/// </summary>
public static class NodeGroupSaveHelper
{
    public const string GROUP_JSON_PROPERTY_NAME = "NodeGroups";

    private static readonly List<string> IgnoredGroups = new List<string>
    {
        "physics_process",
        "process",
        "idle_process",
    };

    public static void WriteGroups(JsonWriter writer, Node value, JsonSerializer serializer)
    {
        writer.WritePropertyName(GROUP_JSON_PROPERTY_NAME);

        var groups = value.GetGroups().Cast<string>().ToList();

        // Ignore inbuilt groups
        groups.RemoveAll(item => item.BeginsWith("_") || IgnoredGroups.Contains(item));

        if (groups.Count > 0)
        {
            serializer.Serialize(writer, groups);
        }
        else
        {
            serializer.Serialize(writer, null);
        }
    }

    public static void ReadGroups(JObject item, Node instance, JsonReader reader, JsonSerializer serializer)
    {
        _ = reader;
        _ = serializer;

        var groupValues = item[GROUP_JSON_PROPERTY_NAME];

        var groups = groupValues?.ToObject<List<string>>();

        if (groups != null)
        {
            foreach (var group in groups)
            {
                instance.AddToGroup(group);
            }
        }
    }

    public static void CopyGroups(Node target, Node source)
    {
        foreach (var item in source.GetGroups())
        {
            target.AddToGroup((string)item);
        }
    }
}
