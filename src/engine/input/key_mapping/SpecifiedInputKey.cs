using System;
using Godot;

public class SpecifiedInputKey : ICloneable
{
    public SpecifiedInputKey()
    {
    }

    public SpecifiedInputKey(InputEventWithModifiers @event)
    {
        Control = @event.Control;
        Alt = @event.Alt;
        Shift = @event.Shift;
        switch (@event)
        {
            case InputEventKey inputKey:
                Type = InputType.Key;
                Code = inputKey.Scancode;
                break;
            case InputEventMouseButton inputMouse:
                Type = InputType.MouseButton;
                Code = (uint)inputMouse.ButtonIndex;
                break;
        }
    }

    public enum InputType
    {
        Key,
        MouseButton,
    }

    public bool Control { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public InputType Type { get; set; }
    public uint Code { get; set; }

    /// <summary>
    ///   Creates a string for the button to show.
    /// </summary>
    /// <returns>A human readable string.</returns>
    public override string ToString()
    {
        var text = string.Empty;

        if (Control)
            text += "Control+";
        if (Alt)
            text += "Alt+";
        if (Shift)
            text += "Shift+";
        if (Type == InputType.Key)
        {
            var enumEntry = (KeyList)Code;
            text += enumEntry.ToString();
        }
        else if (Type == InputType.MouseButton)
        {
            text += Code switch
            {
                1 => "Left mouse",
                2 => "Right mouse",
                3 => "Middle mouse",
                4 => "Wheel up",
                5 => "Wheel down",
                6 => "Wheel left",
                7 => "Wheel right",
                8 => "Special 1 mouse",
                9 => "Special 2 mouse",
                _ => "Unknown mouse",
            };
        }

        return text;
    }

    public InputEventWithModifiers ToInputEvent()
    {
        InputEventWithModifiers result = Type switch
        {
            InputType.Key => new InputEventKey { Scancode = Code },
            InputType.MouseButton => new InputEventMouseButton { ButtonIndex = (int)Code },
            _ => throw new NotSupportedException("Unsupported InputType given"),
        };

        result.Alt = Alt;
        result.Control = Control;
        result.Shift = Shift;
        return result;
    }

    public object Clone()
    {
        return new SpecifiedInputKey
        {
            Alt = Alt,
            Code = Code,
            Control = Control,
            Shift = Shift,
            Type = Type,
        };
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (!(obj is SpecifiedInputKey other))
            return false;

        return Control == other.Control &&
            Alt == other.Alt &&
            Shift == other.Shift &&
            Type == other.Type &&
            Code == other.Code;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Control.GetHashCode();
            hashCode = (hashCode * 397) ^ Alt.GetHashCode();
            hashCode = (hashCode * 397) ^ Shift.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Type;
            hashCode = (hashCode * 397) ^ (int)Code;
            return hashCode;
        }
    }
}
