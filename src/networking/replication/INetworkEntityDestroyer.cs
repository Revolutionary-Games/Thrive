using Arch.Core;

/// <summary>
///   Destroys entities a client no longer needs
/// </summary>
public interface INetworkEntityDestroyer
{
    public void DestroyEntity(in Entity entity);
}
