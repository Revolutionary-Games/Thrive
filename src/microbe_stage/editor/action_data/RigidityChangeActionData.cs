using System;

[JSONAlwaysDynamicType]
public class RigidityChangeActionData : MicrobeEditorActionData
{
    public float NewRigidity;
    public float PreviousRigidity;

    public RigidityChangeActionData(float newRigidity, float previousRigidity)
    {
        NewRigidity = newRigidity;
        PreviousRigidity = previousRigidity;
    }

    public override MicrobeActionInterferenceMode GetInterferenceModeWith(MicrobeEditorActionData other)
    {
        if (other is RigidityChangeActionData rigidityChangeActionData)
        {
            if (Math.Abs(NewRigidity - rigidityChangeActionData.PreviousRigidity) < MathUtils.EPSILON &&
                Math.Abs(rigidityChangeActionData.NewRigidity - PreviousRigidity) < MathUtils.EPSILON)
                return MicrobeActionInterferenceMode.CancelsOut;

            if (Math.Abs(NewRigidity - rigidityChangeActionData.PreviousRigidity) < MathUtils.EPSILON ||
                Math.Abs(rigidityChangeActionData.NewRigidity - PreviousRigidity) < MathUtils.EPSILON)
                return MicrobeActionInterferenceMode.Combinable;
        }

        return MicrobeActionInterferenceMode.NoInterference;
    }

    public override int CalculateCost()
    {
        return (int)Math.Abs((NewRigidity - PreviousRigidity) * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO) *
            Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
    }

    protected override MicrobeEditorActionData CombineGuaranteed(MicrobeEditorActionData other)
    {
        var rigidityChangeActionData = (RigidityChangeActionData)other;

        if (Math.Abs(PreviousRigidity - rigidityChangeActionData.NewRigidity) < MathUtils.EPSILON)
            return new RigidityChangeActionData(NewRigidity, rigidityChangeActionData.PreviousRigidity);

        return new RigidityChangeActionData(rigidityChangeActionData.NewRigidity, PreviousRigidity);
    }
}
