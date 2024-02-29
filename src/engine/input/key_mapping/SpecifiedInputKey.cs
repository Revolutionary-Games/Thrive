using System;
using System.Text;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Represents a single button, along with modifiers, used to trigger some action
/// </summary>
public class SpecifiedInputKey : ICloneable
{
    private StringBuilder? toStringBuilder;

    [JsonConstructor]
    public SpecifiedInputKey()
    {
    }

    public SpecifiedInputKey(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventKey inputKey:
                ConstructFrom(inputKey);
                return;
            case InputEventMouseButton inputMouse:
                ConstructFrom(inputMouse);
                return;
            case InputEventJoypadButton inputControllerButton:
                if (inputControllerButton.ButtonIndex < 0)
                    throw new ArgumentException("Controller button index is invalid");

                Type = InputType.ControllerButton;
                Code = PackCodeWithDevice((int)inputControllerButton.ButtonIndex, inputControllerButton.Device);
                break;

            case InputEventJoypadMotion inputControllerAxis:
                Type = InputType.ControllerAxis;
                Code = PackAxisWithDirection((int)inputControllerAxis.Axis, inputControllerAxis.AxisValue,
                    inputControllerAxis.Device);
                break;

            default:
                throw new ArgumentException("Unknown type of event to convert to input key");
        }

        Control = false;
        Alt = false;
        Shift = false;
    }

    public enum InputType
    {
        Key,
        MouseButton,
        ControllerButton,
        ControllerAxis,
    }

    public bool Control { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public InputType Type { get; set; }
    public uint Code { get; set; }

    [JsonIgnore]
    public bool PrefersGraphicalRepresentation =>
        Type is InputType.ControllerAxis or InputType.ControllerButton or InputType.MouseButton;

    public Control GenerateGraphicalRepresentation()
    {
        var container = new HBoxContainer();

        switch (Type)
        {
            case InputType.ControllerButton:
            {
                var (button, device) = UnpackCodeAndDevice(Code);

                container.AddChild(CreateTextureRect(KeyPromptHelper.GetPathForControllerButton((JoyButton)button)));

                if (device >= 0)
                    GD.Print("TODO: displaying device restriction");

                break;
            }

            case InputType.ControllerAxis:
            {
                var overlayPositioner = new CenterContainer
                {
                    MouseFilter = Godot.Control.MouseFilterEnum.Ignore,
                };

                var (axis, direction, device) = UnpackAxis(Code);

                overlayPositioner.AddChild(CreateTextureRect(KeyPromptHelper.GetPathForControllerAxis((JoyAxis)axis)));

                var directionImage = KeyPromptHelper.GetPathForControllerAxisDirection((JoyAxis)axis, direction);

                if (directionImage != null)
                {
                    overlayPositioner.AddChild(CreateTextureRect(directionImage));
                }

                if (device >= 0)
                    GD.Print("TODO: displaying device restriction");

                container.AddChild(overlayPositioner);
                break;
            }

            case InputType.MouseButton:
            {
                var overlayPositioner = new CenterContainer
                {
                    MouseFilter = Godot.Control.MouseFilterEnum.Ignore,
                };

                var (primary, overlay) = KeyPromptHelper.GetPathForMouseButton((MouseButton)Code);

                if (overlay != null)
                {
                    overlayPositioner.AddChild(CreateTextureRect(primary, true));
                    overlayPositioner.AddChild(CreateTextureRect(overlay));
                }
                else
                {
                    overlayPositioner.AddChild(CreateTextureRect(primary));
                }

                container.AddChild(overlayPositioner);
                break;
            }

            default:
                container.AddChild(new Label
                {
                    Text = ToString(),
                    MouseFilter = Godot.Control.MouseFilterEnum.Ignore,
                });
                break;
        }

        return container;
    }

    public InputEvent ToInputEvent()
    {
        switch (Type)
        {
            case InputType.Key or InputType.MouseButton:
                return ToInputEventWithModifiers();

            case InputType.ControllerButton:
            {
                var (button, device) = UnpackCodeAndDevice(Code);
                return new InputEventJoypadButton
                {
                    ButtonIndex = button,
                    Device = device,
                };
            }

            case InputType.ControllerAxis:
            {
                var (axis, direction, device) = UnpackAxis(Code);

                return new InputEventJoypadMotion
                {
                    Axis = axis,
                    AxisValue = direction,
                    Device = device,
                };
            }

            default:
                throw new NotSupportedException("Unsupported InputType to convert to an event");
        }
    }

    public override bool Equals(object? obj)
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

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Control.GetHashCode();
            hashCode = hashCode * 397 ^ Alt.GetHashCode();
            hashCode = hashCode * 397 ^ Shift.GetHashCode();
            hashCode = hashCode * 397 ^ (int)Type;
            hashCode = hashCode * 397 ^ (int)Code;
            return hashCode;
        }
    }

    /// <summary>
    ///   Creates a string for the button to show.
    /// </summary>
    /// <returns>A human readable string.</returns>
    public override string ToString()
    {
        if (toStringBuilder == null)
        {
            toStringBuilder = new StringBuilder();
        }
        else
        {
            toStringBuilder.Clear();
        }

        if (Control)
        {
            toStringBuilder.Append(Localization.Translate("CTRL"));
            toStringBuilder.Append('+');
        }

        if (Alt)
        {
            toStringBuilder.Append(Localization.Translate("ALT"));
            toStringBuilder.Append('+');
        }

        if (Shift)
        {
            toStringBuilder.Append(Localization.Translate("SHIFT"));
            toStringBuilder.Append('+');
        }

        if (Type == InputType.Key)
        {
            // If the key is not defined in KeyNames.cs, the string will just be returned unmodified by Translate()
            toStringBuilder.Append(KeyNames.Translate((Key)Code));
        }
        else if (Type == InputType.MouseButton)
        {
            toStringBuilder.Append(Code switch
            {
                1 => Localization.Translate("LEFT_MOUSE"),
                2 => Localization.Translate("RIGHT_MOUSE"),
                3 => Localization.Translate("MIDDLE_MOUSE"),
                4 => Localization.Translate("WHEEL_UP"),
                5 => Localization.Translate("WHEEL_DOWN"),
                6 => Localization.Translate("WHEEL_LEFT"),
                7 => Localization.Translate("WHEEL_RIGHT"),
                8 => Localization.Translate("SPECIAL_MOUSE_1"),
                9 => Localization.Translate("SPECIAL_MOUSE_2"),
                _ => Localization.Translate("UNKNOWN_MOUSE"),
            });
        }
        else if (Type == InputType.ControllerAxis)
        {
            var (axis, direction, device) = UnpackAxis(Code);
            toStringBuilder.Append(KeyNames.TranslateAxis(axis, direction, KeyPromptHelper.ActiveControllerType));

            if (device != -1)
            {
                toStringBuilder.Append(" Device ");
                toStringBuilder.Append(device + 1);
            }
            else
            {
                toStringBuilder.Append(' ');
                toStringBuilder.Append(Localization.Translate("CONTROLLER_ANY_DEVICE"));
            }
        }
        else if (Type == InputType.ControllerButton)
        {
            var (button, device) = UnpackCodeAndDevice(Code);
            toStringBuilder.Append(KeyNames.TranslateControllerButton(button, KeyPromptHelper.ActiveControllerType));

            if (device != -1)
            {
                toStringBuilder.Append(" Device ");
                toStringBuilder.Append(device + 1);
            }
            else
            {
                toStringBuilder.Append(' ');
                toStringBuilder.Append(Localization.Translate("CONTROLLER_ANY_DEVICE"));
            }
        }
        else
        {
            toStringBuilder.Append("Invalid input key type");
        }

        return toStringBuilder.ToString();
    }

    /// <summary>
    ///   Packs an int and a sign into a single uint
    /// </summary>
    /// <param name="axis">The axis to pack, note that this needs to be less than 14 bits</param>
    /// <param name="value">The direction value to pack</param>
    /// <param name="device">The device to pack in, note that this needs to be less than 15 bits</param>
    /// <returns>The packed value</returns>
    private static uint PackAxisWithDirection(int axis, float value, int device)
    {
        // For direction we just need to preserve the sign
        uint result = value < 0 ? 1U : 0U;

        // For axis we preserve the sign with one bit
        if (axis < 0)
        {
            result |= (0x1 << 1) | ((uint)(axis * -1) << 2);
        }
        else
        {
            result |= (uint)axis << 2;
        }

        // For device we also preserve a sign bit
        if (device < 0)
        {
            result = (result & 0xffff) | (0x1 << 16) | ((uint)(device * -1) << 17);
        }
        else
        {
            result = (result & 0xffff) | ((uint)device << 17);
        }

        return result;
    }

    private static (JoyAxis Axis, float Direction, int Device) UnpackAxis(uint packed)
    {
        float direction = 1;

        if ((packed & 0x1) != 0)
        {
            direction *= -1;
        }

        int axis = (int)((packed & 0xfffc) >> 2);

        if ((packed & 0x2) != 0)
        {
            axis *= -1;
        }

        int device = (int)((packed & 0xffff0000) >> 17);

        if ((packed & 0x10000) != 0)
        {
            device *= -1;
        }

        return ((JoyAxis)axis, direction, device);
    }

    /// <summary>
    ///   Packs a code along with a device identifier
    /// </summary>
    /// <param name="code">The input code to pack, needs to be 15 bytes or less</param>
    /// <param name="device">The device, needs to be 15 bytes or less</param>
    /// <returns>The packed value</returns>
    private static uint PackCodeWithDevice(int code, int device)
    {
#if DEBUG
        if (code > 0xffff)
            throw new ArgumentException("Code too long to pack");
#endif

        uint result;

        // For code we preserve the sign with one bit
        if (code < 0)
        {
            result = 0x1 | ((uint)(code * -1) << 1);
        }
        else
        {
            result = (uint)code << 1;
        }

        // For device we also preserve a sign bit
        if (device < 0)
        {
            result = (result & 0xffff) | (0x1 << 16) | ((uint)(device * -1) << 17);
        }
        else
        {
            result = (result & 0xffff) | ((uint)device << 17);
        }

        return result;
    }

    private static (JoyButton Code, int Device) UnpackCodeAndDevice(uint packed)
    {
        int code = (int)((packed & 0xfffe) >> 1);

        if ((packed & 0x1) != 0)
        {
            code *= -1;
        }

        int device = (int)((packed & 0xffff0000) >> 17);

        if ((packed & 0x10000) != 0)
        {
            device *= -1;
        }

        return ((JoyButton)code, device);
    }

    // TODO: proper unit testing
    private static void TestCodePacking()
    {
        if (UnpackCodeAndDevice(PackCodeWithDevice(0, -1)) != (0, -1))
            throw new Exception();

        if (UnpackCodeAndDevice(PackCodeWithDevice(5, -1)) != ((JoyButton)5, -1))
            throw new Exception();

        if (UnpackCodeAndDevice(PackCodeWithDevice(-5, -1)) != ((JoyButton)(-5), -1))
            throw new Exception();

        if (UnpackCodeAndDevice(PackCodeWithDevice(5, 5)) != ((JoyButton)5, 5))
            throw new Exception();

        if (UnpackCodeAndDevice(PackCodeWithDevice(155, 128)) != ((JoyButton)155, 128))
            throw new Exception();

        if (UnpackAxis(PackAxisWithDirection(1, -1, -1)) != ((JoyAxis)1, -1, -1))
            throw new Exception();

        if (UnpackAxis(PackAxisWithDirection(-1, -1, -1)) != ((JoyAxis)(-1), -1, -1))
            throw new Exception();

        if (UnpackAxis(PackAxisWithDirection(1, 1, -1)) != ((JoyAxis)1, 1, -1))
            throw new Exception();

        if (UnpackAxis(PackAxisWithDirection(5, 1, -1)) != ((JoyAxis)5, 1, -1))
            throw new Exception();

        if (UnpackAxis(PackAxisWithDirection(5, 1, 15)) != ((JoyAxis)5, 1, 15))
            throw new Exception();

        if (UnpackAxis(PackAxisWithDirection(150, 1, 128)) != ((JoyAxis)150, 1, 128))
            throw new Exception();
    }

    private void ConstructFrom(InputEventWithModifiers @event)
    {
        Control = @event.IsCommandOrControlPressed();
        Alt = @event.AltPressed;
        Shift = @event.ShiftPressed;
        switch (@event)
        {
            case InputEventKey inputKey:
                Type = InputType.Key;
                Code = (uint)inputKey.Keycode;
                break;
            case InputEventMouseButton inputMouse:
                Type = InputType.MouseButton;
                Code = (uint)inputMouse.ButtonIndex;
                break;
            default:
                throw new ArgumentException("Unknown type of event to convert to input key");
        }
    }

    private InputEventWithModifiers ToInputEventWithModifiers()
    {
        InputEventWithModifiers result = Type switch
        {
            InputType.Key => new InputEventKey { Keycode = (Key)Code },
            InputType.MouseButton => new InputEventMouseButton { ButtonIndex = (MouseButton)Code },
            _ => throw new NotSupportedException("Unsupported InputType given"),
        };

        result.AltPressed = Alt;
        result.CtrlPressed = Control;
        result.ShiftPressed = Shift;
        return result;
    }

    private TextureRect CreateTextureRect(string image, bool small = false)
    {
        return new TextureRect
        {
            Texture = GD.Load<Texture2D>(image),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            CustomMinimumSize = small ? new Vector2(14, 14) : new Vector2(32, 32),
        };
    }
}
