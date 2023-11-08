namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;


    /// <summary>
    ///   Runs biological processes on entities
    /// </summary>
    [With(typeof(CompoundStorage))]
    [With(typeof(BioProcesses))]
    public sealed class MicrobeProcessManagerSystem : AEntitySetSystem<float>
    {
        
        private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
        private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");

        public MicrobeProcessManagerSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var storage = ref entity.Get<CompoundStorage>();
            ref var processes = ref entity.Get<BioProcesses>();

            ManageProcesses(ref processes, ref storage, delta);
        }

        private void ManageProcesses(ref BioProcesses processor, ref CompoundStorage storage, float delta)
        {
            var bag = storage.Compounds;

            if (processor.ActiveProcesses != null)
            {
                foreach (var process in processor.ActiveProcesses)
                {
                    var processData = process.Process;

                    if(processData.InternalName == "calvin_cycle")
                    {

                    }
                }
            }
        }
    }
}
