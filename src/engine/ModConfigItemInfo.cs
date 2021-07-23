using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ModConfigItemInfo : Resource
{
    public string ID { get; set; }

    [JsonProperty("Display Name")]
    public string DisplayName { get; set; }

    public string Description { get; set; }

    public string Type { get; set; }

    // Exclusively for Enums/Option type
    public object Options { get; set; }

    public object Value { get; set; }

    [JsonProperty("Min")]
    public float MinimumValue { get; set; } = 0f;

    [JsonProperty("Max")]
    public float MaximumValue { get; set; } = 99f;

    public override bool Equals(object other)
    {
        var item = other as ModConfigItemInfo;

        if (item == null)
        {
            return false;
        }

        return ID == item.ID && Type == item.Type;
    }

    public List<string> GetAllOptions()
    {
        var optionsJArray = Options as JArray;
        return optionsJArray.ToObject<List<string>>();
    }

    public override int GetHashCode()
    {
        return (ID, Type).GetHashCode();
    }
}
