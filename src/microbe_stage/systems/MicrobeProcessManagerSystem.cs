namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Controls the speed of biological processes on a microbe
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If you are looking for the actual implementation of processes, see <see cref="Systems.ProcessSystem"/>
    ///   </para>
    /// </remarks>
    [With(typeof(CompoundStorage))]
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
            ref var storage = ref entity.Get<CompoundStorage>();
            ref var processes = ref entity.Get<BioProcesses>();

            ManageProcesses(ref processes, ref storage, delta);
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

        private void ManageProcesses(ref BioProcesses processor, ref CompoundStorage storage, float delta)
        {
            // Make sure there are ActiveProcesses
            if (processor.ActiveProcesses == null)
                return;

            if (biome == null)
                throw new NullReferenceException("Biome needs to be set");

            var glucoseConsuming = false;

            // Create dummy compound bag
            var bag = storage.Compounds.Clone();

            // Sort processes in order of priority
            // TODO: Possibly find a way to not run this on every call
            var processPriority = processor.ActiveProcesses
                .OrderBy(p => GetPriority(p.Process)).ToList();

            processor.ActiveProcesses = processPriority;

            foreach (var process in processor.ActiveProcesses)
            {
                process.Rate = 1;
            }

            foreach (var process in processor.ActiveProcesses)
            {
                var processData = process.Process;

                // This is used for any other changes to the rate
                var speedModifier = 1.0f;

                var environmentModifier = ProcessSystem.CalculateEnvironmentModifier(processData, null, biome);

                // Constrains the speed of the process to not exceed or overuse storage
                var storageConstraintModifier = ProcessSystem.CalculateStorageModifier(process, null, environmentModifier,
                    delta, bag);

                var rate = speedModifier;
                var totalModifier = rate * environmentModifier * storageConstraintModifier;

                // Set process rate if above minimum
                process.Rate = totalModifier < Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION ? 0 : rate;

                if (process.Rate == 0)
                    continue;

                totalModifier *= delta * process.Count;

                // Consume inputs from dummy bag
                foreach (var entry in processData.Inputs)
                {
                    if (entry.Key.IsEnvironmental)
                        continue;

                    if (entry.Key == Glucose && totalModifier >= MathUtils.EPSILON)
                    {
                        glucoseConsuming = true;
                    }

                    var inputRemoved = entry.Value * totalModifier;
                    bag.TakeCompound(entry.Key, inputRemoved);
                }

                // Add outputs to dummy bag
                foreach (var entry in processData.Outputs)
                {
                    if (entry.Key.IsEnvironmental)
                        continue;

                    var outputGenerated = entry.Value * totalModifier;
                    bag.AddCompound(entry.Key, outputGenerated);
                }

                bag.ClampNegativeCompoundAmounts();
                bag.FixNaNCompounds();
            }

            if (glucoseConsuming)
            {
                var ruBisCo = processor.ActiveProcesses.SingleOrDefault(
                    p => p.Process.InternalName == "calvin_cycle");

                if (ruBisCo != null)
                    ruBisCo.Rate = 0;
            }
        }
    }
}
