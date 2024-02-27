using System;
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

    private static readonly List<string> IgnoredGroups = new()
    {
        "physics_process",
        "process",
        "idle_process",
    };

    public static void WriteGroups(JsonWriter writer, Node value, JsonSerializer serializer)
    {
        writer.WritePropertyName(GROUP_JSON_PROPERTY_NAME);

        try
        {
            var groups = value.GetGroups().Cast<string>().ToList();

            // Ignore inbuilt groups
            groups.RemoveAll(g => g.StartsWith("_") || IgnoredGroups.Contains(g));

            serializer.Serialize(writer, groups.Count > 0 ? groups : null);
        }
        catch (ObjectDisposedException e)
        {
            throw new JsonException($"Failed to save Node groups ({value}) due to the object being disposed", e);
        }
    }

    public static void ReadGroups(InProgressObjectDeserialization objectLoad, Node instance)
    {
        var (name, value, _, _) = objectLoad.GetCustomProperty(GROUP_JSON_PROPERTY_NAME);

        if (name != null && value != null)
        {
            foreach (var group in (JArray)value)
            {
                instance.AddToGroup(group.ValueNotNull<string>());
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
