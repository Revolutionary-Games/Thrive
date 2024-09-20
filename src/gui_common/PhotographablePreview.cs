﻿using Godot;

/// <summary>
///   Base class for displaying photograph tasks
/// </summary>
/// <remarks>
///   <para>
///     To use this, instantiate its scene, extend this class and decide:
///     <list type="bullet">
///       <item>
///         When to update preview by calling UpdatePreview()
///       </item>
///       <item>
///         What to preview by overriding SetupImageTask()
///       </item>
///     </list>
///   </para>
/// </remarks>
[GodotAbstract]
public partial class PhotographablePreview : Control
{
    /// <summary>
    ///   If true then the plain <see cref="Image"/> version of the preview texture is also kept in memory
    /// </summary>
    [Export]
    public bool KeepPlainImageInMemory;

#pragma warning disable CA2213
    [Export]
    private TextureRect textureRect = null!;

    private Texture2D? loadingTexture;

    // TODO: conclude if we should dispose this or if caching might in the future share these
    private Image? finishedImage;
#pragma warning restore CA2213

    private ImageTask? task;

    protected PhotographablePreview()
    {
    }

    public override void _Ready()
    {
        base._Ready();

        InitLoadingTexture();

        // This is to prevent the loading texture from shown before the first photograph is added.
        if (task == null)
            textureRect.Texture = null;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (task?.Finished == true)
        {
            textureRect.Texture = task.FinalImage;

            if (KeepPlainImageInMemory)
            {
                finishedImage = task.PlainImage;
            }
            else
            {
                finishedImage = null;
            }

            task = null;
        }
    }

    /// <summary>
    ///   Returns the finished plain image when ready and <see cref="KeepPlainImageInMemory"/> was true when the image
    ///   generation started
    /// </summary>
    /// <returns>The image or null</returns>
    public Image? GetFinishedImageIfReady()
    {
        return finishedImage;
    }

    protected void UpdatePreview()
    {
        // Make sure calling this before _Ready (for example when setting up before adding to the scene tree) works
        InitLoadingTexture();

        textureRect.Texture = loadingTexture;
        task = SetupImageTask();

        if (task != null)
            PhotoStudio.Instance.SubmitTask(task);
    }

    /// <summary>
    ///   Return an ImageTask ready for PhotoStudio to process
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Do NOT submit the task in this function
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <code>
    ///     protected override ImageTask? SetupImageTask()
    ///     {
    ///         return conditionValid ? new ImageTask(new IScenePhotographable()) : null;
    ///     }
    ///   </code>
    /// </example>
    /// <returns>An image task, or null if condition not satisfied</returns>
    protected virtual ImageTask? SetupImageTask()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    private void InitLoadingTexture()
    {
        loadingTexture ??= textureRect.Texture;
    }
}
