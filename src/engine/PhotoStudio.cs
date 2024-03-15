using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Creates images of game resources by rendering them in a separate viewport
/// </summary>
/// <remarks>
///   <para>
///     TODO: implement a persistent resource cache class that can store these created images (it should be cleared
///     if game version changes to avoid bugs and size limited to not take too many gigabytes of space, and also
///     delete old resources after like 30 days)
///   </para>
/// </remarks>
public partial class PhotoStudio : SubViewport
{
    [Export]
    public NodePath? CameraPath;

    [Export]
    public NodePath RenderedObjectHolderPath = null!;

    [Export]
    public NodePath SimulationWorldsParentPath = null!;

    [Export]
    public bool UseBackgroundSceneLoad;

    [Export]
    public bool UseBackgroundSceneInstance;

    [Export]
    public float SimulationWorldTimeAdvanceStep = 1 / 30.0f;

    private static PhotoStudio? instance;

    private readonly Dictionary<ISimulationPhotographable.SimulationType, IWorldSimulation> worldSimulations = new();
    private readonly Dictionary<IWorldSimulation, Node3D> simulationWorldRoots = new();

    private readonly Queue<ImageTask> tasks = new();
    private ImageTask? currentTask;
    private Step currentTaskStep = Step.NoTask;

    private bool waitingForBackgroundOperation;

#pragma warning disable CA2213

    /// <summary>
    ///   This holds the final rendered image across some steps, this is not disposed as this is passed out as the
    ///   result object
    /// </summary>
    private Image? renderedImage;

    private Node3D? instancedScene;

    private Camera3D camera = null!;
    private Node3D renderedObjectHolder = null!;

    private Node simulationWorldsParent = null!;

    private PackedScene? taskScene;

    // This is not disposed as this is contained in a list, the contents of which are disposed
    private IWorldSimulation? previouslyUsedWorldSimulation;
#pragma warning restore CA2213

    private string? loadedTaskScene;
    private bool previousSceneWasCorrect;

    private PhotoStudio() { }

    private enum Step
    {
        NoTask,
        LoadScene,
        InstanceScene,
        ApplySceneParameters,
        AttachScene,
        WaitSceneStabilize,
        PositionCamera,
        Render,
        CaptureImage,
        Save,
        Cleanup,
    }

    public static PhotoStudio Instance => instance ?? throw new InstanceNotLoadedYetException();

    private bool TaskUsesWorldSimulation => currentTask?.SimulationPhotographable != null;

    /// <summary>
    ///   Calculates a good camera distance from the radius of an object that is photographed
    /// </summary>
    /// <param name="radius">The radius of the object</param>
    /// <returns>The distance to use</returns>
    public static float CameraDistanceFromRadiusOfObject(float radius)
    {
        if (radius <= 0)
            throw new ArgumentException("radius needs to be over 0");

        // TODO: figure out if the camera FOV or FOV / 2 is the right thing to use here
        float angle = Constants.PHOTO_STUDIO_CAMERA_FOV;

        // Some right angle triangle math that's hopefully right
        return Mathf.Tan(MathUtils.DEGREES_TO_RADIANS * angle) * radius;
    }

    public override void _Ready()
    {
        instance = this;

        base._Ready();

        camera = GetNode<Camera3D>(CameraPath);
        renderedObjectHolder = GetNode<Node3D>(RenderedObjectHolderPath);
        simulationWorldsParent = GetNode(SimulationWorldsParentPath);

        // We manually trigger rendering when we want
        RenderTargetUpdateMode = UpdateMode.Disabled;

        camera.Fov = Constants.PHOTO_STUDIO_CAMERA_FOV;

        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        if (currentTaskStep == Step.NoTask)
        {
            // Try to start a task or do nothing if there aren't any
            if (tasks.Count > 0)
            {
                currentTask = tasks.Dequeue();
                currentTaskStep = Step.LoadScene;
                previousSceneWasCorrect = false;
            }
        }

        switch (currentTaskStep)
        {
            case Step.NoTask:
                break;
            case Step.LoadScene:
            {
                if (TaskUsesWorldSimulation)
                {
                    LoadCurrentTaskWorldSimulation();
                }
                else if (UseBackgroundSceneLoad)
                {
                    if (!waitingForBackgroundOperation)
                    {
                        waitingForBackgroundOperation = true;
                        TaskExecutor.Instance.AddTask(new Task(LoadCurrentTaskScene));
                    }
                }
                else
                {
                    LoadCurrentTaskScene();
                }

                break;
            }

            case Step.InstanceScene:
            {
                if (TaskUsesWorldSimulation)
                {
                    // Make sure the used simulation is visible
                    simulationWorldRoots[previouslyUsedWorldSimulation!].Visible = true;

                    // Remove the old scene if a scene type thing was last photographed
                    loadedTaskScene = null;
                    instancedScene?.QueueFree();
                    instancedScene = null;

                    currentTaskStep = Step.ApplySceneParameters;
                }
                else if (UseBackgroundSceneInstance)
                {
                    if (!waitingForBackgroundOperation)
                    {
                        waitingForBackgroundOperation = true;
                        TaskExecutor.Instance.AddTask(new Task(InstanceCurrentScene));
                    }
                }
                else
                {
                    InstanceCurrentScene();
                }

                break;
            }

            case Step.ApplySceneParameters:
            {
                if (TaskUsesWorldSimulation)
                {
                    currentTask!.SimulationPhotographable!.SetupWorldEntities(previouslyUsedWorldSimulation!);
                }
                else
                {
                    // If a simulation is used, hide that
                    if (previouslyUsedWorldSimulation != null)
                    {
                        simulationWorldRoots[previouslyUsedWorldSimulation].Visible = false;
                        previouslyUsedWorldSimulation = null;
                    }

                    currentTask!.ScenePhotographable!.ApplySceneParameters(instancedScene ??
                        throw new Exception("scene was not instanced when expected"));
                }

                currentTaskStep = Step.AttachScene;
                break;
            }

            case Step.AttachScene:
            {
                if (TaskUsesWorldSimulation)
                {
                    // Run a step to start things happening with the simulation
                    previouslyUsedWorldSimulation!.ProcessLogic(SimulationWorldTimeAdvanceStep);
                }
                else
                {
                    // Only need to swap scenes if the new image is of a different type of thing than what we had
                    // previously
                    if (!previousSceneWasCorrect)
                    {
                        renderedObjectHolder.FreeChildren();
                        renderedObjectHolder.AddChild(instancedScene);
                    }
                }

                currentTaskStep = Step.WaitSceneStabilize;
                break;
            }

            case Step.WaitSceneStabilize:
            {
                if (TaskUsesWorldSimulation)
                {
                    // Wait until simulation no longer has any pending operations
                    if (previouslyUsedWorldSimulation!.HasSystemsWithPendingOperations() ||
                        !currentTask!.SimulationPhotographable!.StateHasStabilized(previouslyUsedWorldSimulation))
                    {
                        previouslyUsedWorldSimulation!.ProcessLogic(SimulationWorldTimeAdvanceStep);
                    }
                    else
                    {
                        // Simulation ready to proceed

                        // Now run the one step with logic frame updates as well in the simulation
                        previouslyUsedWorldSimulation.ProcessAll(SimulationWorldTimeAdvanceStep);

                        currentTaskStep = Step.PositionCamera;
                    }
                }
                else
                {
                    // Need to wait one frame for the objects to initialize
                    currentTaskStep = Step.PositionCamera;
                }

                break;
            }

            case Step.PositionCamera:
            {
                if (TaskUsesWorldSimulation)
                {
                    camera.Position =
                        currentTask!.SimulationPhotographable!.CalculatePhotographDistance(
                            previouslyUsedWorldSimulation!);
                }
                else
                {
                    camera.Position = currentTask!.ScenePhotographable!.CalculatePhotographDistance(instancedScene!);
                }

                currentTaskStep = Step.Render;
                break;
            }

            case Step.Render:
            {
                // Cause a render to happen on this frame from our camera
                RenderTargetUpdateMode = UpdateMode.Once;
                currentTaskStep = Step.CaptureImage;
                break;
            }

            case Step.CaptureImage:
            {
                renderedImage = GetTexture().GetImage();
                currentTaskStep = Step.Save;
                break;
            }

            case Step.Save:
            {
                renderedImage!.Convert(Image.Format.Rgba8);

                // TODO: should mipmaps be optional?
                renderedImage.GenerateMipmaps();

                var texture = ImageTexture.CreateFromImage(renderedImage);

                currentTask!.OnFinished(texture, renderedImage);
                currentTask = null;

                currentTaskStep = Step.Cleanup;
                renderedImage = null;

                break;
            }

            case Step.Cleanup:
            {
                // Cleanup used world simulation (if any)
                previouslyUsedWorldSimulation?.DestroyAllEntities();

                currentTaskStep = Step.NoTask;
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        base._Process(delta);
    }

    /// <summary>
    ///   Starts an image creation task
    /// </summary>
    /// <param name="task">The task to queue and run as soon as possible</param>
    public void SubmitTask(ImageTask task)
    {
        tasks.Enqueue(task);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CameraPath != null)
            {
                CameraPath.Dispose();
                RenderedObjectHolderPath.Dispose();
                SimulationWorldsParentPath.Dispose();
            }

            foreach (var entry in worldSimulations)
            {
                entry.Value.Dispose();
            }

            worldSimulations.Clear();

            foreach (var entry in simulationWorldRoots)
            {
                // This is needed to not cause warnings when shutting down the game as apparently the worlds have been
                // destroyed already by Godot
                if (IsInstanceValid(entry.Value))
                    entry.Value.QueueFree();
            }

            simulationWorldRoots.Clear();

            previouslyUsedWorldSimulation = null;
        }

        base.Dispose(disposing);
    }

    private void LoadCurrentTaskScene()
    {
        var wantedScenePath = currentTask!.ScenePhotographable!.SceneToPhotographPath;

        if (wantedScenePath == loadedTaskScene)
        {
            previousSceneWasCorrect = true;
            currentTaskStep = Step.ApplySceneParameters;
        }
        else
        {
            taskScene = GD.Load<PackedScene>(wantedScenePath);
            loadedTaskScene = wantedScenePath;
            previousSceneWasCorrect = false;
            currentTaskStep = Step.InstanceScene;
        }

        waitingForBackgroundOperation = false;
    }

    private void InstanceCurrentScene()
    {
        instancedScene = taskScene!.Instantiate<Node3D>();

        waitingForBackgroundOperation = false;
        currentTaskStep = Step.ApplySceneParameters;
    }

    private void LoadCurrentTaskWorldSimulation()
    {
        var nextSimulation =
            GetOrCreateWorldSimulationForType(currentTask!.SimulationPhotographable!.SimulationToPhotograph);

        if (previouslyUsedWorldSimulation != nextSimulation && previouslyUsedWorldSimulation != null)
        {
            // Switching simulations, hide the previous one
            simulationWorldRoots[previouslyUsedWorldSimulation].Visible = false;
        }

        previouslyUsedWorldSimulation = nextSimulation;
        currentTaskStep = Step.InstanceScene;
    }

    private IWorldSimulation GetOrCreateWorldSimulationForType(ISimulationPhotographable.SimulationType type)
    {
        if (worldSimulations.TryGetValue(type, out var existing))
            return existing;

        switch (type)
        {
            case ISimulationPhotographable.SimulationType.MicrobeGraphics:
            {
                var simulation = new MicrobeVisualOnlySimulation();
                simulation.Init(CreateNewRoot(simulation));

                return worldSimulations[type] = simulation;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private Node3D CreateNewRoot(IWorldSimulation worldSimulation)
    {
        var node = new Node3D();
        simulationWorldsParent.AddChild(node);
        simulationWorldRoots[worldSimulation] = node;

        return node;
    }
}
