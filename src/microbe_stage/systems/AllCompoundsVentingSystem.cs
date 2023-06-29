namespace Systems
{
    using System;
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Vents all compounds until empty from a <see cref="CompoundStorage"/> that has a <see cref="CompoundVenter"/>.
    ///   Requires a <see cref="WorldPosition"/>
    /// </summary>
    [With(typeof(CompoundVenter))]
    [With(typeof(CompoundStorage))]
    [With(typeof(WorldPosition))]
    public sealed class AllCompoundsVentingSystem : AEntitySetSystem<float>
    {
        private readonly CompoundCloudSystem compoundCloudSystem;
        private readonly WorldSimulation worldSimulation;

        private readonly List<Compound> processedCompoundKeys = new();

        public AllCompoundsVentingSystem(CompoundCloudSystem compoundClouds, WorldSimulation worldSimulation,
            World world, IParallelRunner runner) :
            base(world, runner)
        {
            compoundCloudSystem = compoundClouds;
            this.worldSimulation = worldSimulation;

            if (runner.DegreeOfParallelism > 1)
                throw new InvalidOperationException("This system can't run in parallel");
        }

        protected override void Update(float delta, in Entity entity)
        {
            // TODO: rate limit updates if needed for performance?

            ref var venter = ref entity.Get<CompoundVenter>();

            if (venter.VentingPrevented)
                return;

            ref var compounds = ref entity.Get<CompoundStorage>();

            if (compounds.Compounds.Compounds.Count < 1)
            {
                // Empty, perform defined actions for when this venter runs out
                OnOutOfCompounds(in entity, ref venter);
                return;
            }

            ref var position = ref entity.Get<WorldPosition>();

            processedCompoundKeys.Clear();
            processedCompoundKeys.AddRange(compounds.Compounds.Compounds.Keys);

            // Loop through all the compounds in the storage bag and eject them
            bool vented = false;
            foreach (var compound in processedCompoundKeys)
            {
                if (compounds.VentChunkCompound(compound, delta * venter.VentEachCompoundPerSecond, position.Position,
                        compoundCloudSystem))
                {
                    vented = true;
                }
            }

            if (!vented)
            {
                OnOutOfCompounds(in entity, ref venter);
            }
        }

        private void OnOutOfCompounds(in Entity entity, ref CompoundVenter venter)
        {
            if (venter.RanOutOfVentableCompounds)
                return;

            // Stop venting
            venter.VentingPrevented = true;
            venter.RanOutOfVentableCompounds = true;

            if (venter.UsesMicrobialDissolveEffect)
            {
                // TODO: do this effect
                throw new NotImplementedException();
            }
            else if (venter.DestroyOnEmpty)
            {
                worldSimulation.DestroyEntity(entity);
            }
        }
    }
}
