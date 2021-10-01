using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Defines a species' personality by holding behaviour properties
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class BehaviourDictionary : IReadOnlyDictionary<BehaviouralValue, float>, ICloneable
{
    [JsonProperty]
    public float Aggression { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    [JsonProperty]
    public float Opportunism { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    [JsonProperty]
    public float Fear { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    [JsonProperty]
    public float Activity { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    [JsonProperty]
    public float Focus { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    [JsonIgnore]
    public int Count => 5;

    public IEnumerable<BehaviouralValue> Keys => new[]
    {
        BehaviouralValue.Aggression,
        BehaviouralValue.Opportunism,
        BehaviouralValue.Fear,
        BehaviouralValue.Activity,
        BehaviouralValue.Focus,
    };

    public IEnumerable<float> Values => new[]
    {
        Aggression,
        Opportunism,
        Fear,
        Activity,
        Focus,
    };

    public float this[BehaviouralValue key]
    {
        get
        {
            if (TryGetValue(key, out var value))
                return value;

            throw new KeyNotFoundException($"{key} is not a valid BehaviouralValue");
        }
        set
        {
            switch (key)
            {
                case BehaviouralValue.Aggression:
                    Aggression = value;
                    break;
                case BehaviouralValue.Opportunism:
                    Opportunism = value;
                    break;
                case BehaviouralValue.Fear:
                    Fear = value;
                    break;
                case BehaviouralValue.Activity:
                    Activity = value;
                    break;
                case BehaviouralValue.Focus:
                    Focus = value;
                    break;
                default:
                    throw new KeyNotFoundException($"{key} is not a valid BehaviouralValue");
            }
        }
    }

    public IEnumerator<KeyValuePair<BehaviouralValue, float>> GetEnumerator()
    {
        yield return new KeyValuePair<BehaviouralValue, float>(BehaviouralValue.Aggression, Aggression);
        yield return new KeyValuePair<BehaviouralValue, float>(BehaviouralValue.Opportunism, Opportunism);
        yield return new KeyValuePair<BehaviouralValue, float>(BehaviouralValue.Fear, Fear);
        yield return new KeyValuePair<BehaviouralValue, float>(BehaviouralValue.Activity, Activity);
        yield return new KeyValuePair<BehaviouralValue, float>(BehaviouralValue.Focus, Focus);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool ContainsKey(BehaviouralValue key)
    {
        return true;
    }

    public bool TryGetValue(BehaviouralValue key, out float value)
    {
        var result = true;
        switch (key)
        {
            case BehaviouralValue.Aggression:
                value = Aggression;
                break;
            case BehaviouralValue.Opportunism:
                value = Opportunism;
                break;
            case BehaviouralValue.Fear:
                value = Fear;
                break;
            case BehaviouralValue.Activity:
                value = Activity;
                break;
            case BehaviouralValue.Focus:
                value = Focus;
                break;
            default:
                result = false;
                value = default;
                break;
        }

        return result;
    }

    public object Clone()
    {
        var obj = new BehaviourDictionary();
        foreach (var pair in this)
            obj[pair.Key] = pair.Value;

        return obj;
    }
}
