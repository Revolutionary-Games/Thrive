﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Creates images of game resources by rendering them in a separate viewport
/// </summary>
/// <remarks>
///   <para>
///     TODO: implement a persistent resource cache class that can store these created images (it should be cleared
///     if game version changes to avoid bugs and size limited to not take too many gigabytes of space)
///   </para>
/// </remarks>
public class PhotoStudio : Viewport
{
    [Export]
    public NodePath CameraPath = null!;

    [Export]
    public NodePath RenderedObjectHolderPath = null!;

    [Export]
    public bool UseBackgroundSceneLoad;

    [Export]
    public bool UseBackgroundSceneInstance;

    private static PhotoStudio? instance;

    private readonly Queue<ImageTask> tasks = new();
    private ImageTask? currentTask;
    private Step currentTaskStep = Step.NoTask;

    private bool waitingForBackgroundOperation;

    private PackedScene? taskScene;
    private string? loadedTaskScene;
    private bool previousSceneWasCorrect;

    private Image? renderedImage;

    private Spatial? instancedScene;

    private Camera camera = null!;
    private Spatial renderedObjectHolder = null!;

    private PhotoStudio() { }

    private enum Step
    {
        NoTask,
        LoadScene,
        InstanceScene,
        ApplySceneParameters,
        AttachScene,
        PositionCamera,
        Render,
        CaptureImage,
        Save,
    }

    public static PhotoStudio Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   Calculates a good camera distance from the radius of an object that is photographed
    /// </summary>
    /// <param name="radius">The radius of the object</param>
    /// <returns>The distance to use</returns>
    public static float CameraDistanceFromRadiusOfObject(float radius)
    {
        if (radius <= 0)
            throw new ArgumentException("radius needs to be over 0");

        float angle = Constants.PHOTO_STUDIO_CAMERA_HALF_ANGLE;

        // Some right angle triangle math that's hopefully right
        return Mathf.Tan(MathUtils.DEGREES_TO_RADIANS * angle) / radius;
    }

    public override void _Ready()
    {
        instance = this;

        base._Ready();

        camera = GetNode<Camera>(CameraPath);
        renderedObjectHolder = GetNode<Spatial>(RenderedObjectHolderPath);

        // We manually trigger rendering when we want
        RenderTargetUpdateMode = UpdateMode.Disabled;

        camera.Fov = Constants.PHOTO_STUDIO_CAMERA_FOV;
    }

    public override void _Process(float delta)
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
                if (UseBackgroundSceneLoad)
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
                if (UseBackgroundSceneInstance)
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
                currentTask!.Photographable.ApplySceneParameters(instancedScene ??
                    throw new Exception("scene was not instanced when expected"));
                currentTaskStep = Step.AttachScene;
                break;
            }

            case Step.AttachScene:
            {
                // Only need to swap scenes if the new image is of a different type of thing than what we had previously
                if (!previousSceneWasCorrect)
                {
                    renderedObjectHolder.FreeChildren();
                    renderedObjectHolder.AddChild(instancedScene);
                }

                currentTaskStep = Step.PositionCamera;
                break;
            }

            case Step.PositionCamera:
            {
                camera.Translation = new Vector3(0,
                    currentTask!.Photographable.CalculatePhotographDistance(instancedScene!), 0);
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
                renderedImage = GetTexture().GetData();
                currentTaskStep = Step.Save;
                break;
            }

            case Step.Save:
            {
                renderedImage!.Convert(Image.Format.Rgba8);

                var texture = new ImageTexture();
                texture.CreateFromImage(renderedImage,
                    (uint)Texture.FlagsEnum.Filter | (uint)Texture.FlagsEnum.Mipmaps);

                currentTask!.OnFinished(texture);
                currentTask = null;

                currentTaskStep = Step.NoTask;
                renderedImage = null;

                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }

        base._Process(delta);

        if (currentTaskStep == Step.PositionCamera)
        {
            // For some reason this needs to be here (instead of in the switch above) to get the rendering
            // to work correctly
            currentTaskStep = Step.Render;
        }
    }

    /// <summary>
    ///   Starts an image creation task
    /// </summary>
    /// <param name="task">The task to queue and run as soon as possible</param>
    public void SubmitTask(ImageTask task)
    {
        tasks.Enqueue(task);
    }

    /// <summary>
    ///   Starts an image creation for a thing that can be photographed
    /// </summary>
    /// <param name="photographable">The object to create and start an <see cref="ImageTask"/> for</param>
    public void SubmitTask(IPhotographable photographable)
    {
        tasks.Enqueue(new ImageTask(photographable));
    }

    private void LoadCurrentTaskScene()
    {
        var wantedScenePath = currentTask!.Photographable.SceneToPhotographPath;

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
        instancedScene = taskScene!.Instance<Spatial>();

        waitingForBackgroundOperation = false;
        currentTaskStep = Step.ApplySceneParameters;
    }
}
