using Godot;

/// <summary>
///   Base interface that all organelle components need to implement
/// </summary>
public interface IOrganelleComponent
{
    public void OnAttachToCell(PlacedOrganelle organelle);

    /// <summary>
    ///   This update is called from multiple threads at once so only operations that aren't timing sensitive between
    ///   multiple objects and don't modify Godot data are allowed. Everything else needs to be in
    ///   <see cref="UpdateSync"/>
    /// </summary>
    /// <param name="delta">Time since the last update in seconds</param>
    public void UpdateAsync(float delta);

    public void UpdateSync();
}
