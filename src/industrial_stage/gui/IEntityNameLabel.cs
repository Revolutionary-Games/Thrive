using Godot;

/// <summary>
///   Concrete name labels used by <see cref="StrategicEntityNameLabelSystem"/> must implement this interface
/// </summary>
public interface IEntityNameLabel
{
    public delegate void OnEntitySelected();

    public event OnEntitySelected? OnEntitySelectedHandler;

    public bool Visible { get; set; }

    public Control LabelControl { get; }

    /// <summary>
    ///   Called periodically to let the label adapt its content to the new entity data
    /// </summary>
    /// <param name="entity">The entity this is showing</param>
    public void UpdateFromEntity(IEntityWithNameLabel entity);
}
