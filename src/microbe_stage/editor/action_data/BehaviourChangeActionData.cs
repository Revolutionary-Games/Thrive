using System;

[JSONAlwaysDynamicType]
public class BehaviourChangeActionData : MicrobeEditorActionData
{
    public float OldValue;
    public float NewValue;
    public BehaviouralValueType Type;

    public BehaviourChangeActionData(float oldValue, float newValue, BehaviouralValueType type)
    {
        OldValue = oldValue;
        NewValue = newValue;
        Type = type;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(ActionData other)
    {
        if (other is BehaviourChangeActionData behaviourChangeActionData && behaviourChangeActionData.Type == Type)
        {
            // If the value has been changed back to a previous value
            if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON &&
                Math.Abs(behaviourChangeActionData.NewValue - OldValue) < MathUtils.EPSILON)
                return MicrobeActionInterferenceMode.CancelsOut;

            // If the value has been changed twice
            if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON ||
                Math.Abs(behaviourChangeActionData.NewValue - OldValue) < MathUtils.EPSILON)
                return MicrobeActionInterferenceMode.Combinable;
        }

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        // TODO: should this be free?
        return 0;
    }

    protected override ActionData CombineGuaranteed(ActionData other)
    {
        var behaviourChangeActionData = (BehaviourChangeActionData)other;
        if (Math.Abs(OldValue - behaviourChangeActionData.NewValue) < MathUtils.EPSILON)
            return new BehaviourChangeActionData(behaviourChangeActionData.OldValue, NewValue, Type);

        return new BehaviourChangeActionData(behaviourChangeActionData.NewValue, OldValue, Type);
    }
}
