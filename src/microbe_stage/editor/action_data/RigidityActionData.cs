using System;

[JSONAlwaysDynamicType]
public class RigidityActionData : EditorCombinableActionData
{
    public float NewRigidity;
    public float PreviousRigidity;

    public RigidityActionData(float newRigidity, float previousRigidity)
    {
        NewRigidity = newRigidity;
        PreviousRigidity = previousRigidity;
    }

    protected override int CalculateCostInternal()
    {
        return (int)Math.Abs((NewRigidity - PreviousRigidity) * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO) *
            Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        if (other is RigidityActionData rigidityChangeActionData)
        {
            // If the value has been changed back to a previous value
            if (Math.Abs(NewRigidity - rigidityChangeActionData.PreviousRigidity) < MathUtils.EPSILON &&
                Math.Abs(rigidityChangeActionData.NewRigidity - PreviousRigidity) < MathUtils.EPSILON)
                return ActionInterferenceMode.CancelsOut;

            // If the value has been changed twice
            if (Math.Abs(NewRigidity - rigidityChangeActionData.PreviousRigidity) < MathUtils.EPSILON ||
                Math.Abs(rigidityChangeActionData.NewRigidity - PreviousRigidity) < MathUtils.EPSILON)
                return ActionInterferenceMode.Combinable;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        var rigidityChangeActionData = (RigidityActionData)other;

        if (Math.Abs(PreviousRigidity - rigidityChangeActionData.NewRigidity) < MathUtils.EPSILON)
            return new RigidityActionData(NewRigidity, rigidityChangeActionData.PreviousRigidity);

        return new RigidityActionData(rigidityChangeActionData.NewRigidity, PreviousRigidity);
    }
}
