using Godot;

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
public abstract class PhotographablePreview : Control
{
    [Export]
    public NodePath TextureRectPath = null!;

    private TextureRect textureRect = null!;
    private ImageTask? task;
    private Texture loadingTexture = null!;

    public override void _Ready()
    {
        base._Ready();

        textureRect = GetNode<TextureRect>(TextureRectPath);
        loadingTexture = textureRect.Texture;

        // This is to prevent the loading texture from shown
        // before the first photograph is added.
        textureRect.Texture = null;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (task?.Finished == true)
        {
            textureRect.Texture = task.FinalImage;
            task = null;
        }
    }

    protected void UpdatePreview()
    {
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
    ///         return conditionValid ? new ImageTask(new IPhotographable()) : null;
    ///     }
    ///   </code>
    /// </example>
    /// <returns>An image task, or null if condition not satisfied</returns>
    protected abstract ImageTask? SetupImageTask();
}
