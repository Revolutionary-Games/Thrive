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
    private List<InputAttribute> allAttributes;

    public InputManager()
    {
        staticInstance = this;

        LoadAttributes(new[] { Assembly.GetExecutingAssembly() });

        PauseMode = PauseModeEnum.Process;
    }

    /// <summary>
    ///   Adds the instance to the list of objects receiving input.
    /// </summary>
    /// <param name="instance">The instance to add</param>
    public static void RegisterInstance(object instance)
    {
        // Find all attributes where the associated method's class matches the instances class
        foreach (var inputAttribute in staticInstance
            .allAttributes
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
        staticInstance.allAttributes.ForEach(attribute => attribute.RemoveInstance(instance));
    }

    /// <summary>
    ///   Used for resetting various InputAttributes to their default states when window focus is lost as key up
    ///   notifications won't be received when unfocused.
    /// </summary>
    public static void OnFocusLost()
    {
        staticInstance.allAttributes.ForEach(p => p.FocusLost());
    }

    /// <summary>
    ///   Calls all OnProcess methods of all input attributes
    /// </summary>
    /// <param name="delta">The time since the last _Process call</param>
    public override void _Process(float delta)
    {
        allAttributes.ForEach(p => p.OnProcess(delta));
    }

    /// <summary>
    ///   Calls all OnInput methods of all attributes.
    ///   Ignores InputEventMouseMotion events.
    ///   Sets the input as consumed, if it was consumed.
    /// </summary>
    /// <param name="event">The event the user fired</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        // Ignore mouse motion
        if (@event is InputEventMouseMotion)
            return;

        var handled = false;

        allAttributes.ForEach(p =>
        {
            if (p.OnInput(@event))
                handled = true;
        });

        // Define input as consumed
        if (handled)
            GetTree().SetInputAsHandled();
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

    /// <summary>
    ///   Searches the given assemblies for any InputAttributes, prepares them and adds them to the allAttributes list.
    /// </summary>
    /// <param name="assemblies">The assemblies to search through</param>
    private void LoadAttributes(IEnumerable<Assembly> assemblies)
    {
        // reset the list
        allAttributes = new List<InputAttribute>();

        // foreach assembly
        foreach (var assembly in assemblies)
        {
            // foreach type
            foreach (var type in assembly.GetTypes())
            {
                // foreach method
                foreach (var methodInfo in type.GetMethods())
                {
                    // get all InputAttributes
                    var attributes = (InputAttribute[])methodInfo.GetCustomAttributes(typeof(InputAttribute), true);
                    if (attributes.Length == 0)
                        continue;

                    // get the RunOnAxisGroupAttribute, if there is one
                    var runOnAxisGroupAttribute =
                        (RunOnAxisGroupAttribute)attributes.FirstOrDefault(p => p is RunOnAxisGroupAttribute);

                    foreach (var attribute in attributes)
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
