using System.Collections.Generic;

/// <summary>
///   Readonly access to behaviour values
/// </summary>
public interface IReadOnlyBehaviourDictionary : IReadOnlyDictionary<BehaviouralValueType, float>
{
    public float Aggression { get; }

    public float Opportunism { get; }

    public float Fear { get; }

    public float Activity { get; }

    public float Focus { get; }

    public BehaviourDictionary Clone()
    {
        // Manual list avoids an enumerator allocation
        var obj = new BehaviourDictionary
        {
            Aggression = Aggression,
            Opportunism = Opportunism,
            Fear = Fear,
            Activity = Activity,
            Focus = Focus,
        };

        return obj;
    }
}
