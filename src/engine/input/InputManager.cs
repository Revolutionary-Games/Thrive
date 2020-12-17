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
    private static InputManager staticInstance;

    /// <summary>
    ///   A list of all loaded attributes
    /// </summary>
    private readonly List<InputAttribute> attributes = new List<InputAttribute>();

    public InputManager()
    {
        staticInstance = this;

        LoadAttributes(new[] { Assembly.GetExecutingAssembly() });

        PauseMode = PauseModeEnum.Process;

        StartTimer();
    }

    /// <summary>
    ///   Adds the instance to the list of objects receiving input.
    /// </summary>
    /// <param name="instance">The instance to add</param>
    public static void RegisterReceiver(object instance)
    {
        // Find all attributes where the associated method's class matches the instances class
        // TODO: check if there is some alternative faster approach to registering instances
        foreach (var inputAttribute in staticInstance
            .attributes
            .Where(p => p.Method.DeclaringType == instance.GetType()))
        {
            inputAttribute.AddInstance(new WeakReference(instance));
        }
    }

    /// <summary>
    ///   Removes the given instance from receiving input.
    /// </summary>
    /// <param name="instance">The instance to remove</param>
    public static void UnregisterReceiver(object instance)
    {
        foreach (var attribute in staticInstance.attributes)
            attribute.RemoveInstance(instance);
    }

    /// <summary>
    ///   Used for resetting various InputAttributes to their default states when window focus is lost as key up
    ///   notifications won't be received when unfocused.
    /// </summary>
    public static void OnFocusLost()
    {
        staticInstance.attributes.ForEach(p => p.FocusLost());
    }

    /// <summary>
    ///   Calls all OnProcess methods of all input attributes
    /// </summary>
    /// <param name="delta">The time since the last _Process call</param>
    public override void _Process(float delta)
    {
        attributes.ForEach(p => p.OnProcess(delta));
    }

    /// <summary>
    ///   Calls the OnInput methods of the attributes where ActivationType is UnhandledInput.
    ///   Ignores InputEventMouseMotion events.
    ///   Sets the input as consumed, if it was consumed.
    /// </summary>
    /// <param name="event">The event the user fired</param>
    public override void _UnhandledInput(InputEvent @event)
    {
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

        var disposed = new List<WeakReference>();
        var instances = attribute.Instances;
        var result = false;

        if (method.IsStatic)
        {
            // Call the method without an instance if it's static
            var invokeResult = method.Invoke(null, parameters);

            if (invokeResult != null && invokeResult is bool asBool)
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
                    // if the WeakReference got disposed
                    disposed.Add(instance);
                    continue;
                }

                bool thisInstanceResult;

                var invokeResult = method.Invoke(instance.Target, parameters);

                if (invokeResult != null && invokeResult is bool asBool)
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

        disposed.ForEach(p => instances.Remove(p));

        return result;
    }

    private void OnInput(bool inputUnhandled, InputEvent @event)
    {
        // Ignore mouse motion
        // TODO: support mouse movement input as well
        if (@event is InputEventMouseMotion)
            return;

        var handled = attributes.Any(
            attribute => (inputUnhandled || !attribute.OnlyUnhandled) && attribute.OnInput(@event));

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
        timer.Connect("timeout", this, "ClearReferences");
        AddChild(timer);
    }

    /// <summary>
    ///   Called every second using a timer
    /// </summary>
    private void ClearReferences()
    {
        staticInstance.attributes.ForEach(p => p.Instances.RemoveAll(x => !x.IsAlive));
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
                        (RunOnAxisGroupAttribute)inputAttributes.FirstOrDefault(p => p is RunOnAxisGroupAttribute);

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
                            attributes.Add(attribute);
                        }
                    }
                }
            }
        }

        attributes.Sort(Comparer<InputAttribute>.Create((x, y) => y.Priority - x.Priority));
    }
}
