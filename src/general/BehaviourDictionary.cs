using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Defines a species' personality by holding behaviour properties
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class BehaviourDictionary : IReadOnlyDictionary<BehaviouralValueType, float>, ICloneable
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

    public IEnumerable<BehaviouralValueType> Keys => new[]
    {
        BehaviouralValueType.Aggression,
        BehaviouralValueType.Opportunism,
        BehaviouralValueType.Fear,
        BehaviouralValueType.Activity,
        BehaviouralValueType.Focus,
    };

    public IEnumerable<float> Values => new[]
    {
        Aggression,
        Opportunism,
        Fear,
        Activity,
        Focus,
    };

    public float this[BehaviouralValueType key]
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
                case BehaviouralValueType.Aggression:
                    Aggression = value;
                    break;
                case BehaviouralValueType.Opportunism:
                    Opportunism = value;
                    break;
                case BehaviouralValueType.Fear:
                    Fear = value;
                    break;
                case BehaviouralValueType.Activity:
                    Activity = value;
                    break;
                case BehaviouralValueType.Focus:
                    Focus = value;
                    break;
                default:
                    throw new KeyNotFoundException($"{key} is not a valid BehaviouralValue");
            }
        }
    }

    public IEnumerator<KeyValuePair<BehaviouralValueType, float>> GetEnumerator()
    {
        yield return new KeyValuePair<BehaviouralValueType, float>(BehaviouralValueType.Aggression, Aggression);
        yield return new KeyValuePair<BehaviouralValueType, float>(BehaviouralValueType.Opportunism, Opportunism);
        yield return new KeyValuePair<BehaviouralValueType, float>(BehaviouralValueType.Fear, Fear);
        yield return new KeyValuePair<BehaviouralValueType, float>(BehaviouralValueType.Activity, Activity);
        yield return new KeyValuePair<BehaviouralValueType, float>(BehaviouralValueType.Focus, Focus);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool ContainsKey(BehaviouralValueType key)
    {
        return true;
    }

    public bool TryGetValue(BehaviouralValueType key, out float value)
    {
        var result = true;
        switch (key)
        {
            case BehaviouralValueType.Aggression:
                value = Aggression;
                break;
            case BehaviouralValueType.Opportunism:
                value = Opportunism;
                break;
            case BehaviouralValueType.Fear:
                value = Fear;
                break;
            case BehaviouralValueType.Activity:
                value = Activity;
                break;
            case BehaviouralValueType.Focus:
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
