using Godot;
using Godot.Collections;

public static class ObjectHelpers
{
    /// <summary>
    ///   Disconnects all connections of the specified signal from this object.
    /// </summary>
    public static void DisconnectSignals(this GodotObject @object, string signal)
    {
        foreach (Dictionary entry in @object.GetSignalList())
        {
            var name = (string)entry["name"];

            if (name != signal)
                continue;

            foreach (Dictionary connection in @object.GetSignalConnectionList(name))
            {
                var connectedSignal = (string)connection["signal"];
                var connectedTarget = (GodotObject)connection["target"];
                var connectedMethod = (string)connection["method"];

                if (@object.IsConnected(connectedSignal, new Callable(connectedTarget, connectedMethod)))
                    @object.Disconnect(connectedSignal, new Callable(connectedTarget, connectedMethod));
            }
        }
    }

    /// <summary>
    ///   Checks first if an <see cref="GodotObject"/> connection has been made, if not this then connects the signal.
    /// </summary>
    public static void CheckAndConnect(this GodotObject @object, string signal, GodotObject target, string method,
        Array? binds = null, uint flags = 0)
    {
        if (!@object.IsConnected(signal, new Callable(target, method)))
            @object.Connect(signal, new Callable(target, method), binds, flags);
    }
}
