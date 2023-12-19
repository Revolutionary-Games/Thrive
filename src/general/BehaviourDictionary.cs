using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Defines a species' personality by holding behaviour properties
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class BehaviourDictionary : IReadOnlyDictionary<BehaviouralValueType, float>, ICloneable
{
    private static IEnumerable<BehaviouralValueType> keys = new[]
    {
        BehaviouralValueType.Aggression,
        BehaviouralValueType.Opportunism,
        BehaviouralValueType.Fear,
        BehaviouralValueType.Activity,
        BehaviouralValueType.Focus,
    };

    [JsonConstructor]
    public BehaviourDictionary()
    {
    }

    public BehaviourDictionary(BehaviourDictionary copyValues)
    {
        foreach (var value in copyValues)
        {
            this[value.Key] = value.Value;
        }
    }

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

    public IEnumerable<BehaviouralValueType> Keys => keys;

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

            throw new KeyNotFoundException($"{key} is not a valid BehaviouralValueType");
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
                    throw new KeyNotFoundException($"{key} is not a valid BehaviouralValueType");
            }
        }
    }

    public static string GetBehaviourLocalizedString(BehaviouralValueType type)
    {
        return type switch
        {
            BehaviouralValueType.Aggression => TranslationServer.Translate("BEHAVIOUR_AGGRESSION"),
            BehaviouralValueType.Opportunism => TranslationServer.Translate("BEHAVIOUR_OPPORTUNISM"),
            BehaviouralValueType.Fear => TranslationServer.Translate("BEHAVIOUR_FEAR"),
            BehaviouralValueType.Activity => TranslationServer.Translate("BEHAVIOUR_ACTIVITY"),
            BehaviouralValueType.Focus => TranslationServer.Translate("BEHAVIOUR_FOCUS"),
            _ => type.ToString(),
        };
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

    public void Mutate(Random random)
    {
        // Variables used in AI to determine general behaviour mutate these
        Aggression = (Aggression + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION)).Clamp(0.0f, Constants.MAX_SPECIES_AGGRESSION);
        Fear = (Fear + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION)).Clamp(0.0f, Constants.MAX_SPECIES_FEAR);
        Activity = (Activity + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION)).Clamp(0.0f, Constants.MAX_SPECIES_ACTIVITY);
        Focus = (Focus + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION)).Clamp(0.0f, Constants.MAX_SPECIES_FOCUS);
        Opportunism = (Opportunism + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION)).Clamp(0.0f, Constants.MAX_SPECIES_OPPORTUNISM);
    }

    public bool TryGetValue(BehaviouralValueType key, out float value)
    {
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
                value = default;
                return false;
        }

        return true;
    }

    public object Clone()
    {
        return CloneObject();
    }

    public BehaviourDictionary CloneObject()
    {
        var obj = new BehaviourDictionary();
        foreach (var pair in this)
            obj[pair.Key] = pair.Value;

        return obj;
    }
}
