using System;
using System.Reflection;
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
    public MethodBase Method { get; private set; }

    /// <summary>
    ///   Whether this method should be called even if the input was marked as handled.
    ///   Default value is true.
    /// </summary>
    public bool OnlyUnhandled { get; set; } = true;

    /// <summary>
    ///   Defines the priority of the input.
    ///   Priority defines which method gets to consume an input if two method match the input.
    /// </summary>
    public int Priority { get; set; }

    public override bool Equals(object obj)
    {
        if (!(obj is InputAttribute attr))
            return false;

        return Equals(attr.Method, Method);
    }

    public override int GetHashCode()
    {
        return Method != null ? Method.GetHashCode() : 0;
    }

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
    public abstract void OnProcess(float delta);

    /// <summary>
    ///   Called when the games window lost it's focus.
    ///   Is used to reset things to their unpressed state.
    /// </summary>
    public abstract void FocusLost();

    /// <summary>
    ///   Sets the associated method. Called by InputManager.LoadAttributes().
    /// </summary>
    /// <param name="method">The method this attribute is associated with</param>
    internal void Init(MethodBase method)
    {
        Method = method;
    }

    /// <summary>
    ///   Call the associated method.
    ///   Calls the method with all of the instances or once if the method is static.
    /// </summary>
    /// <param name="parameters">The parameters the method will be called with</param>
    /// <returns>Returns whether the event was consumed or not. Methods returning a boolean can control this.</returns>
    protected bool CallMethod(params object[] parameters)
    {
        return InputManager.CallMethod(this, parameters);
    }
}
