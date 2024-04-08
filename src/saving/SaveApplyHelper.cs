using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
///   Copies properties from a save loaded object copy
/// </summary>
public static class SaveApplyHelper
{
    /// <summary>
    ///   Copies the properties and fields that the Thrive JSON converter saves into JSON
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This doesn't clone cloneable properties, so the source object should not be used anymore after this as
    ///     reference properties can get changed in it after target starts running.
    ///   </para>
    ///   <para>
    ///     TODO: make this respect AssignOnlyChildItemsOnDeserializeAttribute to handle nested such objects
    ///   </para>
    /// </remarks>
    /// <param name="target">Object to set the properties on</param>
    /// <param name="source">Object to copy things from</param>
    /// <param name="ignoreMembers">Member names to skip copying</param>
    public static void CopyJSONSavedPropertiesAndFields(object target, object source,
        List<string>? ignoreMembers = null)
    {
        ignoreMembers ??= new List<string>();

        var type = target.GetType();

        // TODO: should this verify that source.GetType() == type?

        foreach (var field in BaseThriveConverter.FieldsOf(target))
        {
            if (IsNameLoadedFromSaveName(field.Name) || BaseThriveConverter.IsIgnoredGodotMember(field.Name, type))
                continue;

            if (ignoreMembers.Contains(field.Name))
                continue;

            // This skips properties that themselves also use the assign only child properties. Those are handled by
            // the JSON converter recursing into those properties and calling us again
            if (field.GetCustomAttribute<AssignOnlyChildItemsOnDeserializeAttribute>() != null)
                continue;

            var value = field.GetValue(source);

            field.SetValue(target, value);
        }

        foreach (var property in BaseThriveConverter.PropertiesOf(target))
        {
            if (IsNameLoadedFromSaveName(property.Name) ||
                BaseThriveConverter.IsIgnoredGodotMember(property.Name, type))
                continue;

            if (ignoreMembers.Contains(property.Name))
                continue;

            if (property.GetCustomAttribute<AssignOnlyChildItemsOnDeserializeAttribute>() != null)
                continue;

            var set = property.GetSetMethodOnDeclaringType() ??
                throw new InvalidOperationException($"Could not find property setter for {property.Name}");

            var value = property.GetValue(source);

            set.Invoke(target, new[] { value });
        }
    }

    /// <summary>
    ///   Used to ignore overwriting the property saying an object was loaded from a save
    /// </summary>
    /// <param name="name">Name to check against</param>
    /// <returns>True if should be skipped</returns>
    public static bool IsNameLoadedFromSaveName(string name)
    {
        return name is "IsLoadedFromSave" or "isLoadedFromSave";
    }

    /// <summary>
    ///   Looks for any function callbacks in the given object for type T and makes sure they point to the new object
    /// </summary>
    /// <param name="callbacksContainingObject">Object to look callbacks as fields and properties in</param>
    /// <param name="newInstance">The new instance all found callbacks on type T should point to</param>
    /// <typeparam name="T">Only callbacks that have an object of this type are updated</typeparam>
    /// <exception cref="InvalidOperationException">If there is an invalid property that can't be updated</exception>
    public static void ReTargetCallbacks<T>(object callbacksContainingObject, T newInstance)
    {
        foreach (var field in BaseThriveConverter.FieldsOf(callbacksContainingObject))
        {
            var value = field.GetValue(callbacksContainingObject) as Delegate;

            if (value == null)
                continue;

            var newValue = ReTargetCallback(value, newInstance);

            if (!value.Equals(newValue))
                field.SetValue(callbacksContainingObject, value);
        }

        foreach (var property in BaseThriveConverter.PropertiesOf(callbacksContainingObject))
        {
            var value = property.GetValue(callbacksContainingObject) as Delegate;

            if (value == null)
                continue;

            var set = property.GetSetMethodOnDeclaringType() ??
                throw new InvalidOperationException($"Could not find property setter for {property.Name}");

            var newValue = ReTargetCallback(value, newInstance);

            if (!value.Equals(newValue))
                set.Invoke(callbacksContainingObject, new object[] { value });
        }
    }

    public static Delegate ReTargetCallback<T>(Delegate callback, T newInstance)
    {
        MethodInfo method;
        object? target;

        if (callback is MulticastDelegate @delegate)
        {
            method = @delegate.Method;
            target = @delegate.Target;
        }
        else
        {
            throw new NotSupportedException("unsupported callback type");
        }

        if (target is T && !target.Equals(newInstance))
        {
            return method.CreateDelegate(typeof(T), newInstance);
        }

        // Targets something else (or already targets the right object), pass back as is
        return callback;
    }
}
