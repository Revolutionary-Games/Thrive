using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

/// <summary>
///   A handler for inputs. Listens for inputs and notifies the input attributes.
/// </summary>
/// <remarks>
///   <para>
///     This is an AutoLoad class.
///   </para>
/// </remarks>
public class InputManager : Node
{
    private static readonly List<WeakReference> DestroyedListeners = new List<WeakReference>();
    private static InputManager? staticInstance;

    /// <summary>
    ///   A list of all loaded attributes
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is sorted based on priority, so if Dictionary doesn't keep key order this will break.
    ///   </para>
    /// </remarks>
    private Dictionary<InputAttribute, List<WeakReference>> attributes = new();

    public InputManager()
    {
        staticInstance = this;

        LoadAttributes(new[] { Assembly.GetExecutingAssembly() });

        PauseMode = PauseModeEnum.Process;

        StartTimer();
    }

    /// <summary>
    ///   Set to true when a rebinding is being performed, used to discard input
    /// </summary>
    public static bool PerformingRebind { get; set; }

    /// <summary>
    ///   Adds the instance to the list of objects receiving input.
    /// </summary>
    /// <param name="instance">The instance to add</param>
    public static void RegisterReceiver(object instance)
    {
        if (staticInstance == null)
            throw new InstanceNotLoadedYetException();

        bool registered = false;

        var reference = new WeakReference(instance);

        // Find all attributes where the associated method's class matches the instances class
        // TODO: check if there is some alternative faster approach to registering instances
        foreach (var inputAttribute in staticInstance
                     .attributes
                     .Where(p => p.Key.Method?.DeclaringType?.IsInstanceOfType(instance) == true))
        {
            inputAttribute.Value.Add(reference);
            registered = true;
        }

        if (!registered)
        {
            GD.PrintErr("Object registered to receive input, but it has no input attributes on its methods");
        }
    }

    /// <summary>
    ///   Removes the given instance from receiving input.
    /// </summary>
    /// <param name="instance">The instance to remove</param>
    public static void UnregisterReceiver(object instance)
    {
        if (staticInstance == null)
            throw new InstanceNotLoadedYetException();

        int removed = 0;

        foreach (var attribute in staticInstance.attributes)
        {
            removed += attribute.Value.RemoveAll(p => !p.IsAlive || p.Target.Equals(instance));
        }

        if (removed < 1)
        {
            GD.PrintErr("Found no instances to unregister input receiving from");
        }
    }

    /// <summary>
    ///   Used for resetting various InputAttributes to their default states when window focus is lost as key up
    ///   notifications won't be received when unfocused.
    /// </summary>
    public static void OnFocusLost()
    {
        if (staticInstance == null)
            throw new InstanceNotLoadedYetException();

        foreach (var attribute in staticInstance.attributes)
            attribute.Key.FocusLost();
    }

    /// <summary>
    ///   Used for Controls to forward mouse events to the InputManager,
    ///   as Controls swallow the MouseEvents if MouseFilter != Ignore.
    /// </summary>
    /// <param name="inputEvent">The event the user fired</param>
    public static void ForwardInput(InputEvent inputEvent)
    {
        if (staticInstance == null)
            throw new InstanceNotLoadedYetException();

        staticInstance._UnhandledInput(inputEvent);
    }

    /// <summary>
    ///   Calls all OnProcess methods of all input attributes
    /// </summary>
    /// <param name="delta">The time since the last _Process call</param>
    public override void _Process(float delta)
    {
        if (staticInstance == null)
            throw new InstanceNotLoadedYetException();

        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        foreach (var attribute in staticInstance.attributes)
            attribute.Key.OnProcess(delta);
    }

    /// <summary>
    ///   Calls the OnInput methods of the attributes where ActivationType is UnhandledInput.
    ///   Ignores InputEventMouseMotion events.
    ///   Sets the input as consumed, if it was consumed.
    /// </summary>
    /// <param name="event">The event the user fired</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (PerformingRebind)
            return;

        OnInput(true, @event);
    }

    /// <summary>
    ///   Calls the OnInput methods of the attributes where ActivationType is Input.
    ///   Ignores InputEventMouseMotion events.
    ///   Sets the input as consumed, if it was consumed.
    /// </summary>
    /// <param name="event">The event the user fired</param>
    public override void _Input(InputEvent @event)
    {
        if (PerformingRebind)
            return;

        OnInput(false, @event);
    }

    public override void _Notification(int focus)
    {
        // If the window goes out of focus, we don't receive the key released events
        // We reset our held down keys if the player tabs out while pressing a key
        if (focus == MainLoop.NotificationWmFocusOut)
        {
            OnFocusLost();
        }
    }

    internal static bool CallMethod(InputAttribute attribute, object[] parameters)
    {
        var method = attribute.Method;

        // Do nothing if no method is associated
        // TODO: it would be safer against mistakes to make it so that only specific attributes can have null method
        if (method == null)
            return true;

        var instances = staticInstance!.attributes[attribute];
        var result = false;

        if (method.IsStatic)
        {
            // Call the method without an instance if it's static
            var invokeResult = method.Invoke(null, parameters);

            if (invokeResult is bool asBool)
            {
                result = asBool;
            }
            else
            {
                result = true;
            }
        }
        else
        {
            // Call the method for each instance
            foreach (var instance in instances)
            {
                if (!instance.IsAlive)
                {
                    // if the WeakReference is no longer valid
                    DestroyedListeners.Add(instance);
                    continue;
                }

                bool thisInstanceResult;
                object invokeResult;

                try
                {
                    invokeResult = method.Invoke(instance.Target, parameters);
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException is ObjectDisposedException)
                    {
                        GD.PrintErr("A disposed object is still registered for input: ", e.InnerException);
                    }
                    else
                    {
                        GD.PrintErr("Failed to perform input method invoke: ", e);
                    }

                    DestroyedListeners.Add(instance);
                    continue;
                }

                if (invokeResult is bool asBool)
                {
                    thisInstanceResult = asBool;
                }
                else
                {
                    thisInstanceResult = true;
                }

                if (thisInstanceResult)
                {
                    result = true;
                }
            }
        }

        DestroyedListeners.ForEach(p => instances.Remove(p));
        DestroyedListeners.Clear();

        return result;
    }

    private void OnInput(bool unhandledInput, InputEvent @event)
    {
        // Ignore mouse motion
        // TODO: support mouse movement input as well
        if (@event is InputEventMouseMotion)
            return;

        bool isDown = @event.IsPressed();

        bool handled = false;

        foreach (var entry in attributes)
        {
            // Skip attributes that have no active listener objects (if this is a key down)
            // TODO: only CallMethod currently removes the invalid references from the list so the object might be dead
            // already and we don't know that yet
            if (isDown && entry.Value.Count < 1)
                continue;

            var attribute = entry.Key;

            if (unhandledInput || !attribute.OnlyUnhandled)
            {
                if (attribute.OnInput(@event))
                {
                    handled = true;

                    // Key releases are passed along to all input listeners, key down is passed to only the first one
                    // that consumes it
                    if (isDown)
                        break;
                }
            }
        }

        // Define input as consumed to Godot if something reacted to it
        if (handled)
            GetTree().SetInputAsHandled();
    }

    private void StartTimer()
    {
        var timer = new Timer
        {
            Autostart = true,
            OneShot = false,
            PauseMode = PauseModeEnum.Process,
            WaitTime = 1,
        };
        timer.Connect("timeout", this, nameof(ClearExpiredReferences));
        AddChild(timer);
    }

    /// <summary>
    ///   Called every second using a timer
    /// </summary>
    private void ClearExpiredReferences()
    {
        foreach (var attributesValue in staticInstance!.attributes.Values)
            attributesValue.RemoveAll(p => !p.IsAlive);
    }

    /// <summary>
    ///   Searches the given assemblies for any InputAttributes, prepares them and adds them to the attributes list.
    /// </summary>
    /// <param name="assemblies">The assemblies to search through</param>
    private void LoadAttributes(IEnumerable<Assembly> assemblies)
    {
        attributes.Clear();

        foreach (var assembly in assemblies)
        {
            // foreach type in the specified assemblies
            foreach (var type in assembly.GetTypes())
            {
                // foreach method in the classes
                foreach (var methodInfo in type.GetMethods())
                {
                    // Check attributes
                    var inputAttributes =
                        (InputAttribute[])methodInfo.GetCustomAttributes(typeof(InputAttribute), true);
                    if (inputAttributes.Length == 0)
                        continue;

                    // Get the RunOnAxisGroupAttribute, if there is one
                    var runOnAxisGroupAttribute =
                        (RunOnAxisGroupAttribute?)inputAttributes.FirstOrDefault(p => p is RunOnAxisGroupAttribute);

                    foreach (var attribute in inputAttributes)
                    {
                        if (runOnAxisGroupAttribute != null && attribute is RunOnAxisAttribute axis)
                        {
                            // Add the axis to the AxisGroup
                            runOnAxisGroupAttribute.AddAxis(axis);
                        }
                        else
                        {
                            // Give the attribute a reference to the method it is placed on
                            attribute.Init(methodInfo);

                            // Add the attribute to the list of all attributes
                            attributes.Add(attribute, new List<WeakReference>());
                        }
                    }
                }
            }
        }

        // Sort the attributes based on priority
        attributes = attributes
            .OrderBy(p => p.Key, Comparer<InputAttribute>.Create((x, y) => y.Priority - x.Priority))
            .ToDictionary(p => p.Key, p => p.Value);
    }
}
