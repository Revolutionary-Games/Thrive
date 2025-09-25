namespace Systems;

using Arch.System;

/// <summary>
///   Adapts a system to be used in a <see cref="Group{T}"/> without it being disposed when the group is.
/// </summary>
public sealed class NonDisposingSystemWrapper<T> : ISystem<T>
{
    private readonly ISystem<T> internalSystem;

    public NonDisposingSystemWrapper(ISystem<T> system)
    {
        internalSystem = system;
    }

    public void Initialize()
    {
        internalSystem.Initialize();
    }

    public void BeforeUpdate(in T t)
    {
        internalSystem.BeforeUpdate(t);
    }

    public void Update(in T t)
    {
        internalSystem.Update(t);
    }

    public void AfterUpdate(in T t)
    {
        internalSystem.Update(t);
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        // Ignore dispose
        _ = disposing;
    }
}
