using Components;
using DefaultEcs;

/// <summary>
///   Base interface that all organelle components need to implement
/// </summary>
public interface IOrganelleComponent
{
    /// <summary>
    ///   When true this component gets sync processing (Godot calls, other non-thread safe things allowed)
    /// </summary>
    public bool UsesSyncProcess { get; }

    public void OnAttachToCell(PlacedOrganelle organelle);

    /// <summary>
    ///   This update is called from multiple threads at once so only operations that aren't timing sensitive between
    ///   multiple objects and don't modify Godot data are allowed. Everything else needs to be in
    ///   <see cref="UpdateSync"/>
    /// </summary>
    /// <param name="organelleContainer">Organelle container instance this organelle is inside</param>
    /// <param name="microbeEntity">Entity reference of the entity that contains this organelle</param>
    /// <param name="delta">Time since the last update in seconds</param>
    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity, float delta);

    /// <summary>
    ///   Sync processing that is allowed to do non-thread safe things (this is called on the main thread). Only called
    ///   if <see cref="UsesSyncProcess"/> is true. For parameter explanations see <see cref="UpdateAsync"/>.
    /// </summary>
    /// <exception cref="System.NotSupportedException">
    ///   If this is called but <see cref="UsesSyncProcess"/> is <c>false</c> derived types are allowed to throw
    ///   this exception
    /// </exception>
    public void UpdateSync(in Entity microbeEntity, float delta);
}
