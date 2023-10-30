namespace Components
{
    using Systems;

    /// <summary>
    ///   Marks entity as being affected by <see cref="FluidCurrentsSystem"/>. Additionally
    ///   <see cref="ManualPhysicsControl"/> and <see cref="WorldPosition"/> are required components.
    ///   This exists as currents need to be skipped for microbes for now as we don't have visualizations for the
    ///   currents.
    /// </summary>
    public struct CurrentAffected
    {
    }
}
