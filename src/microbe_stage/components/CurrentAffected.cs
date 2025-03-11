namespace Components;

using Systems;

/// <summary>
///   Marks entity as being affected by <see cref="FluidCurrentsSystem"/>. Additionally
///   <see cref="ManualPhysicsControl"/> and <see cref="WorldPosition"/> are required components.
///   This exists as currents need to be skipped for microbes for now as we don't have visualizations for the
///   currents.
/// </summary>
[JSONDynamicTypeAllowed]
public struct CurrentAffected
{
    /// <summary>
    ///   Currents' effect on this entity. Note that 0 means the same as 1, being the default constructor value,
    ///   while -1 disables currents' effect.
    /// </summary>
    public float EffectStrength;

    public CurrentAffected(float effectStrength)
    {
        EffectStrength = effectStrength;
    }
}
