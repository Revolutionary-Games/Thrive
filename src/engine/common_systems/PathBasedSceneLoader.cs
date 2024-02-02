namespace Systems
{
    using System;
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Loader for <see cref="PathLoadedSceneVisuals"/> into a <see cref="SpatialInstance"/>
    /// </summary>
    [With(typeof(PathLoadedSceneVisuals))]
    [With(typeof(SpatialInstance))]
    [RunsOnMainThread]
    public sealed class PathBasedSceneLoader : AEntitySetSystem<float>
    {
        /// <summary>
        ///   This stores all the scenes seen in this world. This is done with the assumption that any once used scene
        ///   will get used again in this world at some point.
        /// </summary>
        private readonly Dictionary<string, PackedScene?> usedScenes = new();

        private PackedScene? errorScene;

        public PathBasedSceneLoader(World world, IParallelRunner runner) : base(world, runner)
        {
            // TODO: will we be able to at some point load Godot scenes in parallel without issues?
            if (runner.DegreeOfParallelism > 1)
                throw new ArgumentException("This system cannot be ran in parallel");
        }

        public override void Dispose()
        {
            Dispose(true);

            // This doesn't have a destructor
            // GC.SuppressFinalize(this);
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var sceneVisuals = ref entity.Get<PathLoadedSceneVisuals>();

            // Skip update if nothing to do
            if (sceneVisuals.ScenePath == sceneVisuals.LastLoadedScene)
                return;

            ref var spatial = ref entity.Get<SpatialInstance>();

            sceneVisuals.LastLoadedScene = sceneVisuals.ScenePath;

            if (sceneVisuals.LastLoadedScene == null)
            {
                // Clearing visuals wanted
                spatial.GraphicalInstance = null;

                // The resource will be deleted by SpatialAttachSystem next time it runs as the node instance reference
                // is gone
                return;
            }

            if (!usedScenes.TryGetValue(sceneVisuals.LastLoadedScene, out var scene))
            {
                scene = LoadScene(sceneVisuals.LastLoadedScene);

                if (scene == null)
                {
                    // Try to fallback to an error scene
                    // If we get different quality levels, they are very unlikely to matter for an error so this
                    // situation doesn't need to be complicated if that kind of thing is added
                    errorScene ??= LoadScene(SimulationParameters.Instance.GetErrorVisual().NormalQualityPath);
                    scene = errorScene;
                }

                usedScenes.Add(sceneVisuals.LastLoadedScene, scene);
            }

            if (scene == null)
            {
                // Even error scene failed
                return;
            }

            // TODO: could add a debug-only leak detector system that checks no leaks persist
            // Note that the above TODO is also in PredefinedVisualLoaderSystem

            try
            {
                var instancedScene = scene.Instance<Spatial>();

                if (sceneVisuals.AttachDirectlyToScene)
                {
                    spatial.GraphicalInstance = instancedScene;
                }
                else
                {
                    // Many scenes require a parent node where scale can be used
                    var parent = new Spatial();
                    parent.AddChild(instancedScene);

                    spatial.GraphicalInstance = parent;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"Godot scene ({sceneVisuals.LastLoadedScene}) doesn't have a Spatial root node visual: ",
                    e);
            }
        }

        private PackedScene? LoadScene(string path)
        {
            return GD.Load<PackedScene>(path);
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
