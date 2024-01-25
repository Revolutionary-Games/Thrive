using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Wrapper class for settings options containing the value and a delegate that provides a callback for
///   when the value is changed.
/// </summary>
public class SettingValue<TValueType> : IAssignableSetting
{
    private TValueType value;

    public SettingValue(TValueType value)
    {
        this.value = value;
    }

    /// <summary>
    ///   Generic delegate used for alerting when a setting value was changed.
    /// </summary>
    public delegate void SettingValueChangedDelegate<TSettingValueType>(TSettingValueType value);

    public event SettingValueChangedDelegate<TValueType>? OnChanged;

    public TValueType Value
    {
        get => value;
        set
        {
            if (!Equals(this.value, value))
            {
                this.value = value;

                OnChanged?.Invoke(value);
            }
        }
    }

    public static implicit operator TValueType(SettingValue<TValueType> value)
    {
        return value.value;
    }

    public static bool operator ==(SettingValue<TValueType>? lhs, SettingValue<TValueType>? rhs)
    {
        return Equals(lhs, rhs);
    }

    public static bool operator !=(SettingValue<TValueType> lhs, SettingValue<TValueType> rhs)
    {
        return !(lhs == rhs);
    }

    /// <summary>
    ///   Casts a parameter object into a SettingValue generic of a matching type (if possible) and then
    ///   copies the value from it.
    /// </summary>
    public void AssignFrom(object obj)
    {
        // Convert the object to the correct concrete type if possible.
        var settingObject = obj as SettingValue<TValueType>;

        if (settingObject == null)
            throw new InvalidOperationException("Attempted to assign a SettingValue with an incorrect object type.");

        Value = settingObject.Value;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (GetType() != obj.GetType())
        {
            return false;
        }

        return Equals((SettingValue<TValueType>)obj);
    }

    public bool Equals(SettingValue<TValueType> obj)
    {
        if (ReferenceEquals(value, null))
        {
            return ReferenceEquals(obj.value, null);
        }

        if (ReferenceEquals(obj.value, null))
            return false;

        if (value is IEnumerable<object> enumerable)
        {
            return enumerable.SequenceEqual((IEnumerable<object>)obj.Value!);
        }

        // Apparently primitive types don't get caught by the above check
        // TODO: find a better way if possible to handle this
        if (value is IEnumerable<float> floatList)
        {
            return floatList.SequenceEqual((IEnumerable<float>)obj.Value!);
        }

        // Fallback for handling any types of enumerable types not caught above
        // Really needed to work with any enum type. Funnily enough strings are enumerable so we need to avoid those
        // here
        if (value is not string && value is IEnumerable genericEnumerable)
        {
            var enumerator1 = genericEnumerable.GetEnumerator();

            // Not disposing the enumerators gives a warning. So this is now done like this to dispose the enumerators
            // if they are disposable.
            using var dispose1 = enumerator1 as IDisposable;

            var enumerator2 = ((IEnumerable)obj.Value!).GetEnumerator();
            using var dispose2 = enumerator2 as IDisposable;

            while (enumerator1.MoveNext())
            {
                if (!enumerator2.MoveNext())
                    return false;

                if (!Equals(enumerator1.Current, enumerator2.Current))
                    return false;
            }

            // Second enumerator should be at the end as well now
            if (enumerator2.MoveNext())
                return false;
        }

        if (!value.Equals(obj.value))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        return 17 ^ (value?.GetHashCode() ?? 9);
    }
}
