using Dictionary = Godot.Collections.Dictionary;
using Object = Godot.Object;

public static class ObjectHelpers
{
    /// <summary>
    ///   Disconnects all connections of the specified signal from this object.
    /// </summary>
    public static void DisconnectSignals(this Object @object, string signal)
    {
        foreach (Dictionary entry in @object.GetSignalList())
        {
            var name = (string)entry["name"];

            if (name != signal)
                continue;

            foreach (Dictionary connection in @object.GetSignalConnectionList(name))
            {
                var connectedSignal = (string)connection["signal"];
                var connectedTarget = (Object)connection["target"];
                var connectedMethod = (string)connection["method"];

                if (@object.IsConnected(connectedSignal, connectedTarget, connectedMethod))
                    @object.Disconnect(connectedSignal, connectedTarget, connectedMethod);
            }
        }
    }
}
