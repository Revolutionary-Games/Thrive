using System;

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

        if (!value.Equals(obj.value))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        return 17 ^ (value?.GetHashCode() ?? 9);
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
}
