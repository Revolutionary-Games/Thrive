namespace Systems;

using System.Collections.Generic;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;

/// <summary>
///   Handles <see cref="RadiationSource"/> sending out radiation to anything that can receive it that is nearby
/// </summary>
[With(typeof(RadiationSource))]
[With(typeof(PhysicsSensor))]
[RunsBefore(typeof(ProcessSystem))]
public sealed class IrradiationSystem : AEntitySetSystem<float>
{
    public IrradiationSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_HIGHER_ENTITIES_PER_THREAD)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var source = ref entity.Get<RadiationSource>();
        ref var sensor = ref entity.Get<PhysicsSensor>();

        if (sensor.SensorBody == null)
        {
            // Ignore invalid configurations
            if (source.Radius <= 0)
                return;

            // Setup detection when missing
            sensor.ActiveArea = CreateDetectorShape(source.Radius);
            sensor.ApplyNewShape = true;
        }
        else
        {
            source.RadiatedEntities ??= new HashSet<Entity>();

            sensor.GetDetectedBodies(source.RadiatedEntities);

            if (source.RadiatedEntities.Count == 0)
                return;

            float radiationAmount = source.RadiationStrength * delta;

            foreach (var radiatedEntity in source.RadiatedEntities)
            {
                // Anything with a compound storage can receive radiation
                if (!radiatedEntity.Has<CompoundStorage>())
                    continue;

                ref var storage = ref radiatedEntity.Get<CompoundStorage>();
                var compounds = storage.Compounds;

                // Though if the storage has no capacity set, then don't add anything. This should filter out
                // drain-only storages like chunks
                if (compounds.NominalCapacity <= 0)
                    continue;

                HandleRadiation(compounds, radiationAmount);
            }
        }
    }

    private PhysicsShape CreateDetectorShape(float sourceRadius)
    {
        return PhysicsShape.CreateSphere(sourceRadius);
    }

    private void HandleRadiation(CompoundBag compounds, float amount)
    {
        compounds.AddCompound(Compound.Radiation, amount);
    }
}
