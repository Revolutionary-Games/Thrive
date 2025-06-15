namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;

/// <summary>
///   Records the player ATP for easy access. Run before osmoregulation to ensure it doesn't flicker.
/// </summary>
[With(typeof(PlayerMarker))]
[With(typeof(CompoundStorage))]
[RunsAfter(typeof(ProcessSystem))]
[RunsBefore(typeof(OsmoregulationAndHealingSystem))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RuntimeCost(0.5f)]
[RunsOnMainThread]
public sealed class PlayerATPRecorderSystem(World world) : AEntitySetSystem<float>(world, null)
{
    public static float ATP { get; private set; } = 0;
    public static float ATPCapacity { get; private set; } = 0;

    protected override void PreUpdate(float delta)
    {
        // Reset values
        ATP = 0;
        ATPCapacity = 0;
    }

    protected override void Update(float delta, in Entity entity)
    {
        // Update values for the player
        ref var storage = ref entity.Get<CompoundStorage>();
        ATP = storage.Compounds.GetCompoundAmount(Compound.ATP);
        ATPCapacity = storage.Compounds.GetCapacityForCompound(Compound.ATP);
    }
}
