using Components;
using DefaultEcs;
using DefaultEcs.System;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Handles the logic for emitting toxins from cells.
/// </summary>
public class ToxinEmissionSystem : AEntitySetSystem<float>
{
    private readonly CompoundCloudSystem cloudSystem;
    private readonly World world;

    public ToxinEmissionSystem(World world, CompoundCloudSystem cloudSystem) : base(world.GetEntities()
        .With<CellProperties>()
        .With<CompoundStorage>()
        .With<ToxinEmitter>()
        .AsSet(), true)
    {
        this.world = world;
        this.cloudSystem = cloudSystem;
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var emitter = ref entity.Get<ToxinEmitter>();
        if (!emitter.Active)
            return;

        ref var cellProperties = ref entity.Get<CellProperties>();

        // Prevent dead cells from firing toxins
        if (cellProperties.IsDead)
            return;

        ref var compoundStorage = ref entity.Get<CompoundStorage>();

        // Skip if no toxin compound is defined
        if (string.IsNullOrEmpty(emitter.EmittedCompound))
            return;

        var compound = SimulationParameters.Instance.GetCompound(emitter.EmittedCompound);
        if (compound == null)
            return;

        var amountAvailable = compoundStorage.GetCompoundAmount(compound);
        if (amountAvailable <= 0)
            return;

        var amountToEmit = Mathf.Min(amountAvailable, emitter.EmissionAmount * delta);
        if (amountToEmit <= 0)
            return;

        // Remove the toxin from storage
        compoundStorage.TakeCompound(compound, amountToEmit);

        // Calculate emission position
        var position = entity.Get<WorldPosition>().Position;
        var direction = entity.Get<Physics>().Velocity.Normalized();
        var emissionPosition = position + (direction * emitter.EmissionOffset);

        // Emit the toxin into the cloud
        cloudSystem.EmitCompound(emissionPosition, compound, amountToEmit, emitter.EmissionRadius);
    }

    /// <summary>
    ///   Tries to fire a burst of toxins from a specific entity (usually triggered by player input).
    /// </summary>
    /// <param name="entity">The entity to fire toxins from</param>
    /// <returns>True if toxins were fired, false otherwise</returns>
    public bool FireToxins(Entity entity)
    {
        if (!entity.Has<CellProperties>() || !entity.Has<CompoundStorage>() || !entity.Has<ToxinEmitter>())
            return false;

        ref var cellProperties = ref entity.Get<CellProperties>();

        // Prevent dead cells from firing toxins
        if (cellProperties.IsDead)
            return false;

        ref var emitter = ref entity.Get<ToxinEmitter>();
        if (!emitter.Active)
            return false;

        ref var compoundStorage = ref entity.Get<CompoundStorage>();

        if (string.IsNullOrEmpty(emitter.EmittedCompound))
            return false;

        var compound = SimulationParameters.Instance.GetCompound(emitter.EmittedCompound);
        if (compound == null)
            return false;

        var amountAvailable = compoundStorage.GetCompoundAmount(compound);
        if (amountAvailable <= 0)
            return false;

        var amountToEmit = Mathf.Min(amountAvailable, emitter.EmissionBurstAmount);
        if (amountToEmit <= 0)
            return false;

        compoundStorage.TakeCompound(compound, amountToEmit);

        var position = entity.Get<WorldPosition>().Position;
        var direction = entity.Get<Physics>().Velocity.Normalized();
        var emissionPosition = position + (direction * emitter.EmissionOffset);

        cloudSystem.EmitCompound(emissionPosition, compound, amountToEmit, emitter.EmissionRadius);
        return true;
    }
}
