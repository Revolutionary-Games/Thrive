namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Controls the speed of biological processes on a microbe
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If you are looking for the actual implementation of processes, see <see cref="Systems.ProcessSystem"/>
    ///   </para>
    /// </remarks>
    [With(typeof(BioProcesses))]
    public sealed class MicrobeProcessManagerSystem : AEntitySetSystem<float>
    {
        private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
        private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
        private Dictionary<BioProcess, float> cachedPriority = new();
        private BiomeConditions? biome;

        public MicrobeProcessManagerSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        /// <summary>
        ///   Sets the biome whose environmental values affect processes
        /// </summary>
        public void SetBiome(BiomeConditions newBiome)
        {
            biome = newBiome;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var processes = ref entity.Get<BioProcesses>();

            ManageProcesses(ref processes, delta);
        }

        private float GetPriority(BioProcess process)
        {
            var key = process;

            if (cachedPriority.TryGetValue(key, out var cached))
            {
                return cached;
            }

            // Lower priorities get to run first, so start at bottom
            // It is important non-ATP-producers run first
            // Different levels of priority "coarseness" are stored in different place values
            var priority = 0.0f;

            var inputAmount = 0.0f;
            foreach (var entry in process.Inputs)
            {
                if (entry.Key.IsEnvironmental)
                    continue;

                inputAmount += entry.Value;
            }

            if (process.Outputs.TryGetValue(ATP, out var atpOut))
            {
                if (inputAmount <= 0)
                {
                    priority = 0;
                }
                else
                {
                    priority = 10 - atpOut / inputAmount / 100;
                }
            }

            // Penalize Glucose consumers to allow RuBisCo to work
            if (process.Inputs.ContainsKey(Glucose))
            {
                priority += 100;
            }

            cachedPriority.Add(key, priority);
            return priority;
        }

        private void ManageProcesses(ref BioProcesses processor, float delta)
        {
            // Make sure there are ActiveProcesses
            if (processor.ActiveProcesses == null)
                return;

            if (processor.LimitedATP == null || processor.GlucoseATP == null)
            {
                processor.LimitedATP = 0;
                processor.GlucoseATP = 0;
            }

            if (biome == null)
                throw new NullReferenceException("Biome needs to be set");

            // Sort processes in order of priority
            // TODO: Possibly find a way to not run this on every call
            var processPriority = processor.ActiveProcesses
                .OrderBy(p => GetPriority(p.Process)).ToList();

            processor.ActiveProcesses = processPriority;

            var ruBisCo = processor.ActiveProcesses.SingleOrDefault(
                p => p.Process.InternalName == "calvin_cycle");

            if (ruBisCo != null && ruBisCo.Process.Inputs.TryGetValue(ATP, out var atpIn))
            {
                var environmentModifier = ProcessSystem.CalculateEnvironmentModifier(ruBisCo.Process, null,
                    biome);

                float maxATP = atpIn * environmentModifier * delta;

                if (processor.GlucoseATP > MathUtils.EPSILON)
                {
                    if (processor.GlucoseATP <= maxATP)
                    {
                        ruBisCo.Rate *= (processor.GlucoseATP / maxATP).Value;
                    }
                    else
                    {
                        ruBisCo.Rate = 0;
                    }
                }
                else if (processor.LimitedATP > MathUtils.EPSILON)
                {
                    var newRate = (processor.LimitedATP / maxATP).Value;
                    ruBisCo.Rate = newRate < 1 ? newRate : 1;
                }

                if (ruBisCo.Rate <= Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION)
                    ruBisCo.Rate = 0;
            }

            // Reset stored values
            processor.GlucoseATP = 0;
            processor.LimitedATP = 0;
        }
    }
}
