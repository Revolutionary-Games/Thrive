namespace AutoEvo;

using System;
using System.Collections.Generic;

public class ChangeBehaviorScore : IMutationStrategy<MicrobeSpecies>
{
    private readonly BehaviorAttribute attribute;
    private readonly float maxChange;

    public ChangeBehaviorScore(BehaviorAttribute attribute, float maxChange)
    {
        this.attribute = attribute;
        this.maxChange = maxChange;
    }

    public enum BehaviorAttribute
    {
        Activity,
        Aggression,
        Opportunism,
        Focus,
        Fear,
    }

    public bool Repeatable => true;

    public List<Tuple<MicrobeSpecies, float>> MutationsOf(MicrobeSpecies baseSpecies, float mp)
    {
        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        var change = (float)new Random().NextDouble() * maxChange;

        if (Math.Abs(change) < 1)
        {
            return [];
        }

        switch (attribute)
        {
            case BehaviorAttribute.Activity:
                newSpecies.Behaviour.Activity = Math.Max(Math.Min(newSpecies.Behaviour.Activity + change,
                    Constants.MAX_SPECIES_ACTIVITY), 0);
                break;
            case BehaviorAttribute.Aggression:
                newSpecies.Behaviour.Aggression = Math.Max(Math.Min(newSpecies.Behaviour.Aggression + change,
                    Constants.MAX_SPECIES_AGGRESSION), 0);
                break;
            case BehaviorAttribute.Opportunism:
                newSpecies.Behaviour.Opportunism = Math.Max(Math.Min(newSpecies.Behaviour.Opportunism + change,
                    Constants.MAX_SPECIES_OPPORTUNISM), 0);
                break;
            case BehaviorAttribute.Fear:
                newSpecies.Behaviour.Fear = Math.Max(Math.Min(newSpecies.Behaviour.Fear + change,
                    Constants.MAX_SPECIES_FEAR), 0);
                break;
            case BehaviorAttribute.Focus:
                newSpecies.Behaviour.Focus = Math.Max(Math.Min(newSpecies.Behaviour.Focus + change,
                    Constants.MAX_SPECIES_FOCUS), 0);
                break;
        }

        return [Tuple.Create(newSpecies, mp)];
    }
}
