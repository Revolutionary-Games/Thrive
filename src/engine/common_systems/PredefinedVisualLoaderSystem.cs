namespace Systems
{
    using System;
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Loads predefined visual instances for entities.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     On average this doesn't take a lot of time, but due to potential load time spikes when this does load
    ///     something this has runtime cost of 1 even though 0.25-0.5 would be more suitable based on raw numbers.
    ///   </para>
    /// </remarks>
    [With(typeof(PredefinedVisuals))]
    [With(typeof(SpatialInstance))]
    [RuntimeCost]
    [RunsOnMainThread]
    public sealed class PredefinedVisualLoaderSystem : AEntitySetSystem<float>
    {
        /// <summary>
        ///   This stores all the scenes seen in this world. This is done with the assumption that any once used scene
        ///   will get used again in this world at some point.
        /// </summary>
        private readonly Dictionary<VisualResourceIdentifier, PackedScene?> usedScenes = new();

        private PackedScene? errorScene;

        // External resource that should not be disposed
#pragma warning disable CA2213
        private SimulationParameters simulationParameters = null!;
#pragma warning restore CA2213

        public PredefinedVisualLoaderSystem(World world) : base(world, null)
        {
            // TODO: will we be able to at some point load Godot scenes in parallel without issues?
            // Also a proper resource manager would basically remove the need for that
        }

        // TODO: this will need a callback for when graphics visual level is updated and this needs to redo all of the
        // loaded graphics (if we add a quality level graphics option)

        public override void Dispose()
        {
            Dispose(true);

            // This doesn't have a destructor
            // GC.SuppressFinalize(this);
        }

        protected override void PreUpdate(float state)
        {
            simulationParameters = SimulationParameters.Instance;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var visuals = ref entity.Get<PredefinedVisuals>();

            // Skip update if nothing to do
            if (visuals.VisualIdentifier == visuals.LoadedInstance)
                return;

            ref var spatial = ref entity.Get<SpatialInstance>();

            visuals.LoadedInstance = visuals.VisualIdentifier;

            if (!usedScenes.TryGetValue(visuals.VisualIdentifier, out var scene))
            {
                scene = LoadVisual(simulationParameters.GetVisualResource(visuals.LoadedInstance));

                if (scene == null)
                {
                    // Try to fallback to an error scene
                    errorScene ??= LoadVisual(simulationParameters.GetErrorVisual());
                    scene = errorScene;
                }

                usedScenes.Add(visuals.VisualIdentifier, scene);
            }

            if (scene == null)
            {
                // Even error scene failed
                return;
            }

            // SpatialAttachSystem will handle deleting the graphics instance if not used

            // TODO: could add a debug-only leak detector system that checks no leaks persist

            try
            {
                spatial.GraphicalInstance = scene.Instance<Spatial>();
            }
            catch (Exception e)
            {
                GD.PrintErr("Predefined visual is not convertible to Spatial: ", e);
            }
        }

        private PackedScene? LoadVisual(VisualResourceData visualResourceData)
        {
            // TODO: visual quality (/ LOD level?)
            return GD.Load<PackedScene>(visualResourceData.NormalQualityPath);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                usedScenes.Clear();
            }
        }
    }
}
