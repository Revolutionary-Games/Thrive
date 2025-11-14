using System;
using System.Collections;
using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   Defines a species' personality by holding behaviour properties
/// </summary>
public class BehaviourDictionary : IArchivable, IReadOnlyBehaviourDictionary
{
    public const ushort SERIALIZATION_VERSION = 1;

    private static IEnumerable<BehaviouralValueType> keys =
    [
        BehaviouralValueType.Aggression,
        BehaviouralValueType.Opportunism,
        BehaviouralValueType.Fear,
        BehaviouralValueType.Activity,
        BehaviouralValueType.Focus,
    ];

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

    public float Aggression { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    public float Opportunism { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    public float Fear { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    public float Activity { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    public float Focus { get; set; } = Constants.DEFAULT_BEHAVIOUR_VALUE;

    public int Count => 5;

    public IEnumerable<BehaviouralValueType> Keys => keys;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.BehaviourDictionary;

    public bool CanBeReferencedInArchive => false;

    public IEnumerable<float> Values =>
    [
        Aggression,
        Opportunism,
        Fear,
        Activity,
        Focus,
    ];

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
            BehaviouralValueType.Aggression => Localization.Translate("BEHAVIOUR_AGGRESSION"),
            BehaviouralValueType.Opportunism => Localization.Translate("BEHAVIOUR_OPPORTUNISM"),
            BehaviouralValueType.Fear => Localization.Translate("BEHAVIOUR_FEAR"),
            BehaviouralValueType.Activity => Localization.Translate("BEHAVIOUR_ACTIVITY"),
            BehaviouralValueType.Focus => Localization.Translate("BEHAVIOUR_FOCUS"),
            _ => type.ToString(),
        };
    }

    public static BehaviourDictionary ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new BehaviourDictionary
        {
            Aggression = reader.ReadFloat(),
            Opportunism = reader.ReadFloat(),
            Fear = reader.ReadFloat(),
            Activity = reader.ReadFloat(),
            Focus = reader.ReadFloat(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Aggression);
        writer.Write(Opportunism);
        writer.Write(Fear);
        writer.Write(Activity);
        writer.Write(Focus);
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
        Aggression = Math.Clamp(Aggression + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION), 0.0f, Constants.MAX_SPECIES_AGGRESSION);
        Fear = Math.Clamp(Fear + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION), 0.0f, Constants.MAX_SPECIES_FEAR);
        Activity = Math.Clamp(Activity + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION), 0.0f, Constants.MAX_SPECIES_ACTIVITY);
        Focus = Math.Clamp(Focus + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION), 0.0f, Constants.MAX_SPECIES_FOCUS);
        Opportunism = Math.Clamp(Opportunism + random.Next(Constants.MIN_SPECIES_PERSONALITY_MUTATION,
            Constants.MAX_SPECIES_PERSONALITY_MUTATION), 0.0f, Constants.MAX_SPECIES_OPPORTUNISM);
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
                value = 0;
                return false;
        }

        return true;
    }

    public void CopyFrom(IReadOnlyBehaviourDictionary other)
    {
        Aggression = other.Aggression;
        Opportunism = other.Opportunism;
        Fear = other.Fear;
        Activity = other.Activity;
        Focus = other.Focus;
    }
}
