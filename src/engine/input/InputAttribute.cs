using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
///   An abstract attribute for handling input methods.
///   Can be applied to a method.
/// </summary>
/// <remarks>
///   <para>
///     As this is abstract you need to use one of the following: <see cref="RunOnKeyDownAttribute"/>,
///     <see cref="RunOnKeyUpAttribute"/>, <see cref="RunOnKeyChangeAttribute"/>
///   </para>
/// </remarks>
public abstract class InputAttribute : Attribute
{
    /// <summary>
    ///   The method this Attribute is applied to
    /// </summary>
    public MethodBase? Method { get; private set; }

    /// <summary>
    ///   Whether this method should be called even if the input was marked as handled.
    ///   Default value is true.
    /// </summary>
    public bool OnlyUnhandled { get; set; } = true;

    /// <summary>
    ///   Defines the priority of the input.
    ///   Priority defines which method gets to consume an input if two method match the input. Higher priority value
    ///   means the input gets processed sooner (inputs are processed in descending order).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///   If true, this input tracks how this is interacted with (keyboard/mouse or controller) and updates
    ///   <see cref="LastUsedInputMethod"/>. This will also add the used input method as a call parameter in the
    ///   callback (after all other normal parameters from the input system).
    /// </summary>
    public bool TrackInputMethod { get; set; }

    /// <summary>
    ///   The input method last used to trigger this input. Only set if <see cref="TrackInputMethod"/> is true
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Even if a different default value is added this, probably doesn't need changing as only really the axis
    ///     that triggers with no input can end up using this value before it is set.
    ///   </para>
    /// </remarks>
    public ActiveInputMethod LastUsedInputMethod { get; protected set; } = ActiveInputMethod.Keyboard;

    /// <summary>
    ///   Processes input event for this attribute
    /// </summary>
    /// <param name="input">The event fired by the user</param>
    /// <returns>Returns whether the input was acted on (/ consumed) or not</returns>
    public abstract bool OnInput(InputEvent input);

    /// <summary>
    ///   Processes input actions that aren't triggered on key events directly
    /// </summary>
    /// <param name="delta">The time since the last call of OnProcess</param>
    public abstract void OnProcess(double delta);

    /// <summary>
    ///   Called when the games window lost it's focus.
    ///   Is used to reset things to their unpressed state.
    /// </summary>
    public abstract void FocusLost();

    /// <summary>
    ///   This class needs a custom equals to work in <see cref="InputManager.attributes"/> but a full value comparison
    ///   would sap way too much performance so we use the references for equality and hash code.
    /// </summary>
    /// <param name="obj">The object to compare against</param>
    /// <returns>True if equal</returns>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(this);
    }

    /// <summary>
    ///   Sets the associated method. Called by InputManager.LoadAttributes().
    /// </summary>
    /// <param name="method">The method this attribute is associated with</param>
    internal void Init(MethodBase? method)
    {
        if (Method != null)
            throw new ArgumentException("Trying to re-initialize attribute with different method");

        Method = method;
    }

    /// <summary>
    ///   Called after game initialization is ready. Can be used to perform post startup actions related to inputs.
    /// </summary>
    internal virtual void OnPostLoad()
    {
    }

    /// <summary>
    ///   Called when the game window size changes. Might be useful in the future for some inputs, if not this can be
    ///   eventually removed if not needed for any input scenarios.
    /// </summary>
    internal virtual void OnWindowSizeChanged()
    {
    }

    /// <summary>
    ///   Call the associated method.
    ///   Calls the method with all of the instances or once if the method is static.
    /// </summary>
    /// <param name="parameters">The parameters the method will be called with</param>
    /// <returns>Returns whether the event was consumed or not. Methods returning a boolean can control this.</returns>
    protected bool CallMethod(object[] parameters)
    {
        return InputManager.CallMethod(this, false, parameters);
    }

    /// <summary>
    ///   Calls the associated method and gives an error if it tries to control the consuming of input.
    /// </summary>
    protected void CallDelayedMethod(object[] parameters)
    {
        InputManager.CallMethod(this, true, parameters);
    }

    // TODO: find a way to avoid the boxing as object for parameters here somehow
    protected void PrepareMethodParameters(ref object[]? parameters, int size, object firstParameter)
    {
        if (parameters == null || parameters.Length != size)
            parameters = new object[size];

        parameters[0] = firstParameter;
    }

    protected void PrepareMethodParametersEmpty(ref object[]? parameters)
    {
        if (parameters == null || parameters.Length != 0)
            parameters = Array.Empty<object>();
    }
}
