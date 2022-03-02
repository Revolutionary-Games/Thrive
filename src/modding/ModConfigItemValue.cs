using System;
using System.Collections.Generic;
using Godot;

public class ModConfigItemValue<T> : Reference
{
    public T ConfigValue;

    public ModConfigItemValue(T newValue)
    {
        ConfigValue = newValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is ModConfigItemValue<T> value &&
               EqualityComparer<T>.Default.Equals(ConfigValue, value.ConfigValue);
    }

    public override int GetHashCode()
    {
        return 1770937724 + EqualityComparer<T>.Default.GetHashCode(ConfigValue);
    }
}
