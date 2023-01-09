﻿using System;
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
    private static readonly List<WeakReference> DestroyedListeners = new();
    private static InputManager? staticInstance;

    private readonly Dictionary<int, float> controllerAxisDeadzones = new();

    /// <summary>
    ///   Used to send just one 0 event for a controller axis that is released and goes into the deadzone
    /// </summary>
    private readonly Dictionary<int, bool> deadzonedControllerAxes = new();

    /// <summary>
    ///   A list of all loaded attributes
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is sorted based on priority, so if Dictionary doesn't keep key order this will break.
    ///   </para>
    /// </remarks>
    private Dictionary<InputAttribute, List<WeakReference>> attributes = new();

    /// <summary>
    ///   The last used input method by the player
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: does this need to default to controller in some cases?
    ///   </para>
    /// </remarks>
    private ActiveInputMethod usedInputMethod = ActiveInputMethod.Keyboard;

    private float inputChangeDelay;
    private bool queuedInputChange;

    /// <summary>
    ///   Used to detect when the used controller
    /// </summary>
    private int? lastUsedControllerId;

    /// <summary>
    ///   Used to detect when controller name changes to check if we should swap the used controller type variable
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: is this better than just detecting the last connected controller type?
    ///   </para>
    /// </remarks>
    private string? lastUsedControllerName;

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

    public static Vector2 WindowSizeForInputs { get; private set; }

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
            if (instance.GetType().GetCustomAttribute<IgnoreNoMethodsTakingInputAttribute>() != null)
                return;

            GD.PrintErr("Object registered to receive input, but it has no input attributes on its methods (type: ",
                instance.GetType().Name, ")");
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
            if (instance.GetType().GetCustomAttribute<IgnoreNoMethodsTakingInputAttribute>() != null)
                return;

            GD.PrintErr("Found no instances to unregister input receiving from (unregistering object of type: ",
                instance.GetType().Name, ")");
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

    public static void OnPostLoad()
    {
        if (staticInstance == null)
            throw new InstanceNotLoadedYetException();

        staticInstance.DoPostLoad();
    }

    public override void _Ready()
    {
        base._Ready();

        Input.Singleton.Connect("joy_connection_changed", this, nameof(OnConnectedControllersChanged));

        DoPostLoad();

        // Detect initial controllers
        var controllers = Input.GetConnectedJoypads();
        if (controllers.Count > 0)
        {
            // Apply button style from initial controller
            try
            {
                int controllerId = (int)controllers[0];
                lastUsedControllerName = Input.GetJoyName(controllerId);
                lastUsedControllerId = controllerId;

                GD.Print("First connected controller is: ", lastUsedControllerName);
                ApplyInputPromptTypes();
            }
            catch (Exception e)
            {
                GD.PrintErr("Startup controller style applying failed: ", e);
            }
        }
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

        if (inputChangeDelay > 0)
        {
            inputChangeDelay -= delta;

            if (inputChangeDelay <= 0)
            {
                inputChangeDelay = 0;

                if (queuedInputChange)
                    ApplyInputPromptTypes();
            }
        }

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

    public override void _Notification(int what)
    {
        // If the window goes out of focus, we don't receive the key released events
        // We reset our held down keys if the player tabs out while pressing a key
        if (what == NotificationWmFocusOut)
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

    /// <summary>
    ///   Performs post load actions for inputs. For example some inputs need to listen for settings changes
    /// </summary>
    private void DoPostLoad()
    {
        LoadControllerDeadzones();

        Settings.Instance.ControllerAxisDeadzoneAxes.OnChanged += _ => LoadControllerDeadzones();

        GetTree().Root.Connect("size_changed", this, nameof(OnWindowSizeChanged));
        UpdateWindowSizeForInputs();

        foreach (var attribute in attributes)
        {
            attribute.Key.OnPostLoad();
        }
    }

    private void OnWindowSizeChanged()
    {
        UpdateWindowSizeForInputs();

        foreach (var attribute in attributes)
        {
            attribute.Key.OnWindowSizeChanged();
        }
    }

    private void UpdateWindowSizeForInputs()
    {
        if (Settings.Instance.InputWindowSizeIsLogicalSize.Value)
        {
            WindowSizeForInputs = LoadingScreen.Instance.LogicalDrawingAreaSize;
        }
        else
        {
            WindowSizeForInputs = OS.WindowSize * OS.GetScreenScale();
        }
    }

    private void OnInput(bool unhandledInput, InputEvent @event)
    {
        UpdateUsedInputMethodType(@event);

        bool isDown = false;

        // For now let's always assume mouse motion is not a "down" action
        if (@event is not InputEventMouseMotion)
        {
            if (@event is InputEventJoypadMotion joypadMotion)
            {
                // Apply controller axis deadzone
                var motionAxis = joypadMotion.Axis;
                controllerAxisDeadzones.TryGetValue(motionAxis, out float deadzone);

                if (Math.Abs(joypadMotion.AxisValue) < deadzone)
                {
                    deadzonedControllerAxes.TryGetValue(motionAxis, out var deadzoned);

                    if (deadzoned)
                    {
                        // Already sent out the deadzone event for this input, don't send again until it changes
                        return;
                    }

                    joypadMotion.AxisValue = 0;
                    deadzonedControllerAxes[motionAxis] = true;
                }
                else
                {
                    deadzonedControllerAxes[motionAxis] = false;
                }

                // TODO: implement maximum value scaling for controller axes (if needed)
            }

            isDown = @event.IsPressed();
        }

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
        // TODO: switch this to using a timer variable like elsewhere in the code
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

    private void UpdateUsedInputMethodType(InputEvent @event)
    {
        ActiveInputMethod? wantedInputMethod = null;
        int? joypadId = null;

        // TODO: should mouse buttons switch the input method or not? In case a user needs to click something to get
        // past something
        if (@event is InputEventKey /* or InputEventMouseButton */)
        {
            wantedInputMethod = ActiveInputMethod.Keyboard;
        }
        else if (@event is InputEventJoypadButton joypadButton)
        {
            if (joypadButton.Device == -1)
            {
                // Emulated mouse
            }
            else
            {
                joypadId = joypadButton.Device;
                wantedInputMethod = ActiveInputMethod.Controller;
            }
        }

        // Exit if we don't know what mode we want to be in
        if (wantedInputMethod == null)
            return;

        // or we are already in the right mode (and also controller mode is right)
        if (wantedInputMethod.Value == usedInputMethod)
        {
            if (usedInputMethod != ActiveInputMethod.Controller || lastUsedControllerId == joypadId)
                return;
        }

        // Skip changing input method if the input is an action that shouldn't change input type
        if (Constants.ActionsThatDoNotChangeInputMethod.Any(a => @event.IsAction(a)))
            return;

        usedInputMethod = wantedInputMethod.Value;

        if (joypadId != null)
        {
            if (lastUsedControllerId != joypadId)
            {
                // Used controller changed
                lastUsedControllerId = joypadId;

                lastUsedControllerName = Input.GetJoyName(lastUsedControllerId.Value);
                GD.Print("Controller name is now: ", lastUsedControllerName);
            }
        }

        // This delay prevents the icons from changing each frame if multiple input types are firing at the same time
        // TODO: it would probably be nice to gradually increase this delay when rapid changes are detected
        if (inputChangeDelay > 0)
        {
            queuedInputChange = true;
        }
        else
        {
            ApplyInputPromptTypes();
        }

        inputChangeDelay = Constants.MINIMUM_DELAY_BETWEEN_INPUT_TYPE_CHANGE;
    }

    private void ApplyInputPromptTypes()
    {
        if (lastUsedControllerName != null)
        {
            // TODO: allow overriding controller button types from the options menu

            KeyPromptHelper.ActiveControllerType =
                ControllerTypeDetection.DetectControllerTypeFromName(lastUsedControllerName);
        }

        KeyPromptHelper.InputMethod = usedInputMethod;
        queuedInputChange = false;
    }

    private void OnConnectedControllersChanged(int device, bool connected)
    {
        // This connected signal doesn't seem to apply during startup, instead only when a controller is reconnected
        if (connected)
        {
            GD.Print($"Controller {device} connected");
        }
        else
        {
            GD.Print($"Controller {device} was disconnected");
        }

        lastUsedControllerId = null;
    }

    private void LoadControllerDeadzones()
    {
        var values = Settings.Instance.ControllerAxisDeadzoneAxes.Value;

        if (values.Count != (int)JoystickList.AxisMax)
        {
            GD.PrintErr("Mismatching number of controller axis deadzones. Expected: ", (int)JoystickList.AxisMax,
                " actually configured: ", values.Count);
        }

        controllerAxisDeadzones.Clear();

        for (int i = 0; i < values.Count; ++i)
        {
            controllerAxisDeadzones[i] = values[i];
        }
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
                // Skip abstract classes as those can't be instantiated and their methods will be detected for the
                // derived types
                if (type.IsAbstract || type.IsInterface)
                    continue;

                // foreach method in the classes
                foreach (var methodInfo in type.GetMethods())
                {
                    // Check attributes (duplicate attributes that may be caused by finding duplicates through
                    // inheritance are skipped)
                    var inputAttributes =
                        ((InputAttribute[])methodInfo.GetCustomAttributes(typeof(InputAttribute), true)).Distinct()
                        .ToArray();
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
