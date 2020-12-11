using System;
using System.Collections.Generic;
using System.Linq;
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
    ///   All instances associated with this InputAttribute
    /// </summary>
    private readonly List<WeakReference> instances = new List<WeakReference>();

    /// <summary>
    ///   All references to instances pending removal
    /// </summary>
    private readonly List<WeakReference> disposed = new List<WeakReference>();

    /// <summary>
    ///   The method this Attribute is applied to
    /// </summary>
    public MethodBase Method { get; private set; }

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
    ///   Adds an instance to the list of associated instances.
    ///   Called by InputManager.RegisterReceiver().
    /// </summary>
    /// <param name="instance">The new instance</param>
    internal void AddInstance(WeakReference instance)
    {
        instances.Add(instance);
    }

    /// <summary>
    ///   Removes an instance from the list of associated instances.
    ///   Called by InputManager.UnregisterReceiver().
    /// </summary>
    /// <param name="instance">The instance to remove</param>
    internal void RemoveInstance(object instance)
    {
        instances.RemoveAll(p => !p.IsAlive || p.Target == instance);
    }

    internal void ClearReferences()
    {
        instances.RemoveAll(p => !p.IsAlive);
    }

    /// <summary>
    ///   Call the associated method.
    ///   Calls the method with all of the instances or once if the method is static.
    /// </summary>
    /// <param name="parameters">The parameters the method will be called with</param>
    /// <returns>Returns whether the event was consumed or not. Methods returning a boolean can control this.</returns>
    protected bool CallMethod(params object[] parameters)
    {
        // Do nothing if no method is associated
        // TODO: it would be safer against mistakes to make it so that only specific attributes can have null method
        if (Method == null)
            return true;

        bool result = false;

        if (Method.IsStatic)
        {
            // Call the method without an instance if it's static
            var invokeResult = Method.Invoke(null, parameters);

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

                var invokeResult = Method.Invoke(instance.Target, parameters);

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
        disposed.Clear();

        return result;
    }
}
