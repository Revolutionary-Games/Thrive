using System;
using Godot;

/// <summary>
///   Represents a single button, along with modifiers, used to trigger some action
/// </summary>
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
            text += TranslationServer.Translate("CTRL") + "+";
        if (Alt)
            text += TranslationServer.Translate("ALT") + "+";
        if (Shift)
            text += TranslationServer.Translate("SHIFT") + "+";

        if (Type == InputType.Key)
        {
            // If the key is not defined in KeyNames.cs, the string will just be returned unmodified by Translate()
            text += KeyNames.Translate(Code);
        }
        else if (Type == InputType.MouseButton)
        {
            text += Code switch
            {
                1 => TranslationServer.Translate("LEFT_MOUSE"),
                2 => TranslationServer.Translate("RIGHT_MOUSE"),
                3 => TranslationServer.Translate("MIDDLE_MOUSE"),
                4 => TranslationServer.Translate("WHEEL_UP"),
                5 => TranslationServer.Translate("WHEEL_DOWN"),
                6 => TranslationServer.Translate("WHEEL_LEFT"),
                7 => TranslationServer.Translate("WHEEL_RIGHT"),
                8 => TranslationServer.Translate("SPECIAL_MOUSE_1"),
                9 => TranslationServer.Translate("SPECIAL_MOUSE_2"),
                _ => TranslationServer.Translate("UNKNOWN_MOUSE"),
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
