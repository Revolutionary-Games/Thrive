using Godot;

public static class ObjectHelpers
{
    /// <summary>
    ///   Checks first if an <see cref="GodotObject"/> connection has been made, if not this then connects the signal.
    /// </summary>
    public static void CheckAndConnect(this GodotObject @object, StringName signal, Callable callable, uint flags = 0)
    {
        if (!@object.IsConnected(signal, callable))
            @object.Connect(signal, callable, flags);
    }
}
