using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Attribute for a method, that gets called when the defined axis is not in its idle state.
///   Can be applied multiple times. <see cref="RunOnAxisGroupAttribute"/> required to distinguish between the axes.
/// </summary>
/// <example>
///   <code>
///     [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f })]
///     public void Zoom(float delta, float value)
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RunOnAxisAttribute : InputAttribute
{
    private readonly bool usesMouse;

    /// <summary>
    ///   All associated inputs for this axis
    /// </summary>
    private List<(RunOnInputWithStrengthAttribute Input, MemberData Data)> inputs = new();

    /// <summary>
    ///   Used to track order keys are pressed down
    /// </summary>
    private int inputNumber;

    private bool useDiscreteKeyInputs;

    private object[]? cachedMethodCallParameters;

    /// <summary>
    ///   Instantiates a new RunOnAxisAttribute.
    /// </summary>
    /// <param name="inputNames">
    ///   All godot input names. May contain <see cref="RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX"/> prefixed
    ///   values for specifying mouse inputs.
    /// </param>
    /// <param name="associatedValues">All associated values. Length must match the inputNames</param>
    /// <exception cref="ArgumentException">Gets thrown when the lengths don't match</exception>
    public RunOnAxisAttribute(string[] inputNames, float[] associatedValues)
    {
        // Preprocess input to handle mouse inputs
        int excessInputs = inputNames.Count(i => i.StartsWith(RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX));

        if (inputNames.Length - excessInputs != associatedValues.Length)
            throw new ArgumentException("input names and associated values have to be the same length");

        RunOnRelativeMouseAttribute.CapturedMouseAxis? nextInputMouse = null;

        int valueIndex = 0;

        foreach (var name in inputNames)
        {
            if (name.StartsWith(RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX))
            {
                if (nextInputMouse != null)
                {
                    throw new ArgumentException("Mouse axis inputs need to be specified between the other inputs " +
                        "(before the action it applies to)");
                }

                nextInputMouse = (RunOnRelativeMouseAttribute.CapturedMouseAxis)Enum.Parse(
                    typeof(RunOnRelativeMouseAttribute.CapturedMouseAxis),
                    name.Substring(RunOnKeyAttribute.CAPTURED_MOUSE_AS_AXIS_PREFIX.Length));

                continue;
            }

            inputs.Add((new RunOnInputWithStrengthAttribute(name), new MemberData(associatedValues[valueIndex])));

            if (nextInputMouse != null)
            {
                // Add a mouse input for this value as well
                inputs.Add((new RunOnRelativeMouseAttribute(nextInputMouse.Value),
                    new MemberData(associatedValues[valueIndex])));
                nextInputMouse = null;

                usesMouse = true;
            }

            ++valueIndex;
        }

        if (nextInputMouse != null)
        {
            throw new ArgumentException(
                "Mouse axis inputs need to be specified before the normal action it applies to");
        }

        // Round to make sure that there isn't a really close number instead of the exactly wanted default value
        DefaultState = (float)Math.Round(associatedValues.Average(), 4);
    }

    /// <summary>
    ///   Special modes that can be specified for this axis when it is used for look (camera turning) type inputs.
    ///   For non-mouse input this pre-applies delta, so <see cref="RunOnAxisGroupAttribute.InvokeWithDelta"/>
    /// </summary>
    public enum LookMode
    {
        NotLooking,
        Yaw,
        Pitch,
    }

    /// <summary>
    ///   The idle state. This is the average value of all the associated input values on this axis
    /// </summary>
    public float DefaultState { get; }

    /// <summary>
    ///   Should the method be invoked when all of this object's inputs are in their idle states
    /// </summary>
    public bool InvokeAlsoWithNoInput { get; set; }

    /// <summary>
    ///   Sets how to handle the axis in terms of look customization settings
    /// </summary>
    public LookMode Look { get; set; } = LookMode.NotLooking;

    // TODO: set this to false by default if this doesn't feel good for controller players
    public bool AllowLookBasedDeltaPreMultiply { get; set; } = true;

    /// <summary>
    ///   If true then the axis members only trigger on key down and repeat. Should not be changed after initialization
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: would be nice to not have to recreate the objects here
    ///     TODO: this is untested for mouse and controller inputs
    ///   </para>
    /// </remarks>
    public bool UseDiscreteKeyInputs
    {
        get => useDiscreteKeyInputs;
        set
        {
            if (useDiscreteKeyInputs == value)
                return;

            useDiscreteKeyInputs = value;

            // Change the objects in inputs used for the key handling to the right type
            var newInputs = new List<(RunOnInputWithStrengthAttribute Input, MemberData Data)>();

            foreach (var entry in inputs)
            {
                if (useDiscreteKeyInputs &&
                    entry.Input is not RunOnInputWithStrengthAndRepeatAttribute and not RunOnRelativeMouseAttribute)
                {
                    newInputs.Add((new RunOnInputWithStrengthAndRepeatAttribute(entry.Input.InputName), entry.Data));
                }
                else if (!useDiscreteKeyInputs &&
                         entry.Input is RunOnInputWithStrengthAndRepeatAttribute repeatAttribute)
                {
                    newInputs.Add((new RunOnInputWithStrengthAttribute(repeatAttribute.InputName), entry.Data));
                }
                else
                {
                    newInputs.Add(entry);
                }
            }

            inputs = newInputs;
        }
    }

    private int NextInputNumber => checked(++inputNumber);

    public override bool OnInput(InputEvent @event)
    {
        var wasUsed = false;

        foreach (var entry in inputs)
        {
            if (entry.Input.OnInput(@event) && entry.Input.HeldDown)
            {
                wasUsed = true;

                try
                {
                    entry.Data.LastDown = NextInputNumber;
                }
                catch (OverflowException)
                {
                    // Reset to a lower value
                    OnInputNumberOverflow();

                    entry.Data.LastDown = 0;
                    inputNumber = 0;
                }
            }
        }

        if (wasUsed && TrackInputMethod)
            LastUsedInputMethod = InputManager.InputMethodFromInput(@event);

        return wasUsed;
    }

    /// <summary>
    ///   Get the currently active axis member value, or DefaultState
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This takes in delta because in specific <see cref="Look"/> modes we need to pre-multiply non-mouse inputs
    ///     with the delta but not mouse inputs. Our result user could not determine that so we need to do the multiply
    ///     by delta.
    ///   </para>
    /// </remarks>
    public float GetCurrentResult(float delta)
    {
        if (!AllowLookBasedDeltaPreMultiply || Look == LookMode.NotLooking)
        {
            delta = 1;
        }
        else
        {
            // Delta is multiplied to be a bit bigger number here to make the results with low delta into a more normal
            // rate
            delta *= 1.5f;
        }

        int highestFoundPressed = int.MinValue;
        (RunOnInputWithStrengthAttribute Input, MemberData Data)? pressedEntry = null;

        foreach (var entry in inputs)
        {
            var lastDown = entry.Data.LastDown;
            var input = entry.Input;
            if (input.ReadHeldOrPrimedAndResetPrimed() && lastDown >= highestFoundPressed)
            {
                if (!input.IsNonControllerEventOrIsStillDown())
                    continue;

                highestFoundPressed = lastDown;
                pressedEntry = entry;
            }
        }

        if (pressedEntry == null)
            return DefaultState;

        var result = pressedEntry.Value.Data.Value * pressedEntry.Value.Input.Strength;

        if (pressedEntry.Value.Input is RunOnRelativeMouseAttribute mouseAttribute)
        {
            // For now we need this bit of a workaround to reset read mouse inputs
            mouseAttribute.MarkMouseMotionRead();
        }
        else
        {
            // Perform delta pre-multiply if needed for this non-mouse input
            result *= delta;
        }

        return result;
    }

    public override void OnProcess(float delta)
    {
        // If UseDiscreteKeyInputs is true CurrentResult evaluation actually changes state, which is not optimal...
        var currentResult = GetCurrentResult(delta);
        if (Math.Abs(currentResult - DefaultState) > MathUtils.EPSILON || InvokeAlsoWithNoInput)
        {
            if (TrackInputMethod)
            {
                PrepareMethodParameters(ref cachedMethodCallParameters, 3, delta);
                cachedMethodCallParameters![1] = currentResult;
                cachedMethodCallParameters![2] = LastUsedInputMethod;
            }
            else
            {
                PrepareMethodParameters(ref cachedMethodCallParameters, 2, delta);
                cachedMethodCallParameters![1] = currentResult;
            }

            CallMethod(cachedMethodCallParameters);
        }
    }

    public override void FocusLost()
    {
        foreach (var entry in inputs)
        {
            entry.Input.FocusLost();
        }
    }

    internal override void OnPostLoad()
    {
        if (usesMouse)
        {
            ApplyWindowSizeScaling();

            Settings.Instance.ScaleMouseInputByWindowSize.OnChanged += _ => ApplyWindowSizeScaling();
            Settings.Instance.InputWindowSizeIsLogicalSize.OnChanged += _ => ApplyWindowSizeScaling();
        }

        if (Look == LookMode.NotLooking)
            return;

        ApplyReverseInputState();
        ApplyInputSensitivityState();

        // Setup listening for reversing inputs and sensitivity
        if (Look == LookMode.Pitch)
        {
            Settings.Instance.InvertVerticalControllerLook.OnChanged += _ => ApplyReverseInputState();
            Settings.Instance.InvertVerticalMouseLook.OnChanged += _ => ApplyReverseInputState();

            Settings.Instance.VerticalMouseLookSensitivity.OnChanged += _ => ApplyInputSensitivityState();
            Settings.Instance.VerticalControllerLookSensitivity.OnChanged += _ => ApplyInputSensitivityState();
        }
        else if (Look == LookMode.Yaw)
        {
            Settings.Instance.InvertHorizontalControllerLook.OnChanged += _ => ApplyReverseInputState();
            Settings.Instance.InvertHorizontalMouseLook.OnChanged += _ => ApplyReverseInputState();

            Settings.Instance.HorizontalMouseLookSensitivity.OnChanged += _ => ApplyInputSensitivityState();
            Settings.Instance.HorizontalControllerLookSensitivity.OnChanged += _ => ApplyInputSensitivityState();
        }
    }

    internal override void OnWindowSizeChanged()
    {
        base.OnWindowSizeChanged();

        if (!usesMouse)
            return;

        ApplyWindowSizeScaling();
    }

    private void OnInputNumberOverflow()
    {
        foreach (var entry in inputs)
            entry.Data.LastDown = -entry.Data.LastDown;
    }

    private void ApplyReverseInputState()
    {
        bool reverseMouse;

        // We can't really tell if this is a controller input or a key press. Same problem exists with the sensitivity
        // so keyboard input share the controller input settings regarding these two things.
        bool reverseController;

        var settings = Settings.Instance;

        if (Look == LookMode.Pitch)
        {
            reverseMouse = settings.InvertVerticalMouseLook.Value;
            reverseController = settings.InvertVerticalControllerLook.Value;
        }
        else if (Look == LookMode.Yaw)
        {
            reverseMouse = settings.InvertHorizontalMouseLook.Value;
            reverseController = settings.InvertHorizontalControllerLook.Value;
        }
        else
        {
            throw new InvalidOperationException($"Shouldn't have gotten here with look mode: {Look}");
        }

        if (reverseMouse || reverseController)
        {
            foreach (var entry in inputs)
            {
                if (entry.Input is RunOnRelativeMouseAttribute)
                {
                    if (reverseMouse)
                    {
                        entry.Data.SetReverseDirection();
                    }
                    else
                    {
                        entry.Data.ResetDirection();
                    }
                }
                else if (reverseController)
                {
                    entry.Data.SetReverseDirection();
                }
                else
                {
                    entry.Data.ResetDirection();
                }
            }
        }
        else
        {
            // Not reversed, revert things to defaults
            foreach (var entry in inputs)
            {
                entry.Data.ResetDirection();
            }
        }
    }

    private void ApplyInputSensitivityState()
    {
        float mouseSensitivity;
        float controllerSensitivity;

        var settings = Settings.Instance;

        if (Look == LookMode.Pitch)
        {
            mouseSensitivity = settings.VerticalMouseLookSensitivity.Value;
            controllerSensitivity = settings.VerticalControllerLookSensitivity.Value;
        }
        else if (Look == LookMode.Yaw)
        {
            mouseSensitivity = settings.HorizontalMouseLookSensitivity.Value;
            controllerSensitivity = settings.HorizontalControllerLookSensitivity.Value;
        }
        else
        {
            throw new InvalidOperationException($"Shouldn't have gotten here with look mode: {Look}");
        }

        foreach (var entry in inputs)
        {
            if (entry.Input is RunOnRelativeMouseAttribute)
            {
                entry.Data.SetSensitivity(mouseSensitivity);
            }
            else
            {
                entry.Data.SetSensitivity(controllerSensitivity);
            }
        }
    }

    private void ApplyWindowSizeScaling()
    {
        float scaling = 1;

        // TODO: should this be moved into RunOnRelativeMouseAttribute or does this make more sense to be in this class
        var setting = Settings.Instance.ScaleMouseInputByWindowSize.Value;
        if (setting != MouseInputScaling.None)
        {
            if (Look == LookMode.Pitch)
            {
                scaling = Constants.BASE_VERTICAL_RESOLUTION_FOR_INPUT / InputManager.WindowSizeForInputs.y;
            }
            else
            {
                // Assume yaw direction stands in also for other mouse input modes well enough

                scaling = Constants.BASE_HORIZONTAL_RESOLUTION_FOR_INPUT / InputManager.WindowSizeForInputs.x;
            }
        }

        if (setting == MouseInputScaling.ScaleReverse)
            scaling = 1.0f / scaling;

        foreach (var entry in inputs)
        {
            if (entry.Input is RunOnRelativeMouseAttribute)
            {
                entry.Data.SetMultiplier(scaling);
            }
        }
    }

    private class MemberData
    {
        public readonly float OriginalValue;
        public float Value;

        public int LastDown;

        /// <summary>
        ///   Sensitivity multiplier applied to <see cref="Value"/>. This is not set in the constructor but instead in
        ///   <see cref="RunOnAxisAttribute.OnPostLoad"/>
        /// </summary>
        private float sensitivity = 1.0f;

        /// <summary>
        ///   An extra multiplier on top of <see cref="sensitivity"/>. This is needed as mouse inputs can have window
        ///   size speed scaling.
        /// </summary>
        private float multiplier = 1.0f;

        private bool reversed;

        public MemberData(float value)
        {
            OriginalValue = value;
            UpdateValue();
        }

        public void ResetDirection()
        {
            reversed = false;
            UpdateValue();
        }

        public void SetReverseDirection()
        {
            reversed = true;
            UpdateValue();
        }

        public void SetSensitivity(float newSensitivity)
        {
            if (newSensitivity < 0)
                throw new ArgumentException("Direction sensitivity needs to be above zero");

            sensitivity = newSensitivity;
            UpdateValue();
        }

        public void SetMultiplier(float newMultiplier)
        {
            if (newMultiplier < 0)
                throw new ArgumentException("Direction multiplier needs to be above zero");

            multiplier = newMultiplier;
            UpdateValue();
        }

        private void UpdateValue()
        {
            if (reversed)
            {
                Value = -OriginalValue * sensitivity * multiplier;
            }
            else
            {
                Value = OriginalValue * sensitivity * multiplier;
            }
        }
    }
}
