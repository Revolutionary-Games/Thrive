using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

/// <summary>
///   A handler for inputs.
///   This is an AutoLoad class.
///   Listens for inputs and notifies the input attributes.
/// </summary>
public class InputManager : Node
{
    private static InputManager singleton;

    /// <summary>
    ///   A list of all loaded attributes
    /// </summary>
    private List<InputAttribute> allAttributes;

    public InputManager()
    {
        singleton = this;
        LoadAttributes(new[] { Assembly.GetExecutingAssembly() });
        PauseMode = PauseModeEnum.Process;
    }

    /// <summary>
    ///   Adds the calling instance to the list of managed instances.
    ///   Used for calling knowing which instances' method an input event should call
    /// </summary>
    /// <param name="instance">The instance to add</param>
    public static void AddInstance(object instance)
    {
        // Find all attributes where the associated method's class matches the instances class
        foreach (var inputAttribute in singleton
            .allAttributes
            .Where(p => p.Method.DeclaringType == instance.GetType())
            .AsParallel())
        {
            inputAttribute.AddInstance(new WeakReference(instance));
        }
    }

    /// <summary>
    ///   Removes the given instance from the list of managed instances.
    /// </summary>
    /// <param name="instance">The instance to remove</param>
    public static void RemoveInstance(object instance)
    {
        singleton.allAttributes.ForEach(attribute =>
        {
            attribute.RemoveInstance(instance);
        });
    }

    /// <summary>
    ///   Used for resetting various InputAttributes to their default states.
    /// </summary>
    public static void FocusLost()
    {
        singleton.allAttributes.AsParallel().ForAll(p => p.FocusLost());
    }

    /// <summary>
    ///   Calls all OnProcess methods of all attributes
    /// </summary>
    /// <param name="delta">The time since the last _Process call</param>
    public override void _Process(float delta)
    {
        allAttributes.AsParallel().ForAll(p => p.OnProcess(delta));
    }

    /// <summary>
    ///   Calls all OnInput methods of all attributes.
    ///   Ignores InputEventMouseMotion events.
    ///   Sets the input as consumed, if it was consumed.
    /// </summary>
    /// <param name="event">The event the user fired</param>
    public override void _Input(InputEvent @event)
    {
        // Ignore mouse motion
        if (@event is InputEventMouseMotion)
            return;

        var result = false;
        allAttributes.AsParallel().ForAll(p =>
        {
            if (p.OnInput(@event))
                result = true;
        });

        // Define input as consumed
        if (result)
            GetTree().SetInputAsHandled();
    }

    /// <summary>
    ///   Searches the given assemblies for any InputAttributes, prepares them and adds them to the allAttributes list.
    /// </summary>
    /// <param name="assemblies">The assemblies to search through</param>
    private void LoadAttributes(IEnumerable<Assembly> assemblies)
    {
        // reset the list
        allAttributes = new List<InputAttribute>();

        // foreach assembly
        foreach (var assembly in assemblies.AsParallel())
        {
            // foreach type
            foreach (var type in assembly.GetTypes().AsParallel())
            {
                // foreach method
                foreach (var methodInfo in type.GetMethods().AsParallel())
                {
                    // get all InputAttributes
                    var attributes = (InputAttribute[])methodInfo.GetCustomAttributes(typeof(InputAttribute), true);
                    if (attributes.Length == 0)
                        continue;

                    // get the RunOnAxisGroupAttribute, if there is one
                    var runOnAxisGroupAttribute =
                        (RunOnAxisGroupAttribute)attributes
                            .AsParallel()
                            .FirstOrDefault(p => p is RunOnAxisGroupAttribute);

                    foreach (var attribute in attributes.AsParallel())
                    {
                        if (runOnAxisGroupAttribute != null && attribute is RunOnAxisAttribute axis)
                        {
                            // add the axis to the AxisGroup
                            runOnAxisGroupAttribute.AddAxis(axis);
                        }
                        else
                        {
                            // add the attribute to the list of all attributes
                            attribute.Init(methodInfo);
                            allAttributes.Add(attribute);
                        }
                    }
                }
            }
        }
    }
}
