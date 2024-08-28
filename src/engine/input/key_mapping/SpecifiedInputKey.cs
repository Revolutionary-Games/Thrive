﻿using System;
using System.Text;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Represents a single button, along with modifiers, used to trigger some action
/// </summary>
public class SpecifiedInputKey : ICloneable
{
    private StringBuilder? toStringBuilder;

    private StringName? marginLeftName;
    private StringName? marginRightName;

    [JsonConstructor]
    public SpecifiedInputKey()
    {
    }

    /// <summary>
    ///   Constructs an input key from an event
    /// </summary>
    /// <param name="event">Event to use</param>
    /// <param name="fromUserInput">Set to true when the user is rebinding inputs and this is from the user</param>
    /// <exception cref="ArgumentException">If something is wrong with the event</exception>
    public SpecifiedInputKey(InputEvent @event, bool fromUserInput)
    {
        switch (@event)
        {
            case InputEventKey inputKey:
                ConstructFrom(inputKey, fromUserInput);
                return;
            case InputEventMouseButton inputMouse:
                ConstructFrom(inputMouse, fromUserInput);
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

        // Variants of "Key"
        PhysicalKey,
        KeyLabel,
    }

    public bool Control { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public InputType Type { get; set; }
    public ulong Code { get; set; }

    [JsonIgnore]
    public bool PrefersGraphicalRepresentation =>
        Type is InputType.ControllerAxis or InputType.ControllerButton or InputType.MouseButton;

    /// <summary>
    ///   Packs an int and a sign into a single uint
    /// </summary>
    /// <param name="axis">The axis to pack, note that this needs to be less than 31 bits</param>
    /// <param name="value">The direction value to pack</param>
    /// <param name="device">The device to pack in, note that this needs to be less than 32 bits</param>
    /// <returns>The packed value</returns>
    public static ulong PackAxisWithDirection(int axis, float value, int device)
    {
        // For the direction we just need to preserve the sign
        ulong result = value < 0 ? 1U : 0U;

        // For the axis we preserve the sign with one bit
        if (axis < 0)
        {
            result |= (0x1 << 1) | ((ulong)(axis * -1) << 2);
        }
        else
        {
            result |= (ulong)axis << 2;
        }

        // For the device we also preserve a sign bit
        if (device < 0)
        {
            result = (result & 0xffffffffL) | (0x1L << 32) | ((ulong)(device * -1) << 33);
        }
        else
        {
            result = (result & 0xffffffffL) | ((ulong)device << 33);
        }

        return result;
    }

    public static (JoyAxis Axis, float Direction, int Device) UnpackAxis(ulong packed)
    {
        float direction = 1;

        if ((packed & 0x1) != 0)
        {
            direction *= -1;
        }

        int axis = (int)((packed & 0xfffcL) >> 2);

        if ((packed & 0x2) != 0)
        {
            axis *= -1;
        }

        int device = (int)((packed & 0xffffffff00000000L) >> 33);

        if ((packed & 0x100000000L) != 0)
        {
            device *= -1;
        }

        return ((JoyAxis)axis, direction, device);
    }

    /// <summary>
    ///   Packs a code along with a device identifier
    /// </summary>
    /// <param name="code">The input code to pack, needs to be 31 bytes or fewer</param>
    /// <param name="device">The device, needs to be 32 bytes or fewer</param>
    /// <returns>The packed value</returns>
    public static ulong PackCodeWithDevice(long code, int device)
    {
#if DEBUG
        if (code >= 0xffffffffL)
            throw new ArgumentException("Code too long to pack");
#endif

        ulong result;

        // For the code we preserve the sign with one bit
        if (code < 0)
        {
            result = 0x1 | ((ulong)(code * -1) << 1);
        }
        else
        {
            result = (ulong)code << 1;
        }

        // For the device we also preserve a sign bit
        if (device < 0)
        {
            result = (result & 0xffffffffL) | (0x1L << 32) | ((ulong)(device * -1) << 33);
        }
        else
        {
            result = (result & 0xffffffffL) | ((ulong)device << 33);
        }

        return result;
    }

    public static (JoyButton Code, int Device) UnpackCodeAndDevice(ulong packed)
    {
        long code = (int)((packed & 0xfffffffeL) >> 1);

        if ((packed & 0x1) != 0)
        {
            code *= -1;
        }

        int device = (int)((packed & 0xffffffff00000000L) >> 33);

        if ((packed & 0x100000000L) != 0)
        {
            device *= -1;
        }

        return ((JoyButton)code, device);
    }

    public Control GenerateGraphicalRepresentation(LabelSettings labelSettings)
    {
        var container = new HBoxContainer();

        // Add extra modifiers in front of the main graphical representation
        if (Shift || Control || Alt)
        {
            if (Type is InputType.ControllerAxis or InputType.ControllerButton)
            {
                GD.PrintErr("Generated a graphical representation for a controller input with a modifier, " +
                    "this is likely very wrong");
            }

            if (toStringBuilder == null)
            {
                toStringBuilder = new StringBuilder();
            }
            else
            {
                toStringBuilder.Clear();
            }

            AppendModifierText(toStringBuilder);

            // Re-use StringNames if this single object is converted to a graphical representation multiple times
            // TODO: could optimally push these even to a higher level code (or maybe make these static?) to reduce
            // further how many times these are used
            marginLeftName ??= new StringName("margin_left");
            marginRightName ??= new StringName("margin_top");

            var labelPositioner = new MarginContainer
            {
                MouseFilter = Godot.Control.MouseFilterEnum.Ignore,
            };
            labelPositioner.AddThemeConstantOverride(marginLeftName, 6);
            labelPositioner.AddThemeConstantOverride(marginRightName, 2);

            labelPositioner.AddChild(new Label
            {
                Text = toStringBuilder.ToString(),
                LabelSettings = labelSettings,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Godot.Control.MouseFilterEnum.Ignore,
            });

            container.AddChild(labelPositioner);
        }

        switch (Type)
        {
            case InputType.ControllerButton:
            {
                var (button, device) = UnpackCodeAndDevice(Code);

                container.AddChild(CreateTextureRect(KeyPromptHelper.GetPathForControllerButton(button)));

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

                overlayPositioner.AddChild(CreateTextureRect(KeyPromptHelper.GetPathForControllerAxis(axis)));

                var directionImage = KeyPromptHelper.GetPathForControllerAxisDirection(axis, direction);

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
            case InputType.Key or InputType.PhysicalKey or InputType.KeyLabel or InputType.MouseButton:
                return ToInputEventWithModifiers();

            case InputType.ControllerButton:
            {
                var (button, device) = UnpackCodeAndDevice(Code);
                return new InputEventJoypadButton
                {
                    ButtonIndex = button,
                    Device = device,
                    Pressed = true,
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
            Type = Type,
            Code = Code,
            Control = Control,
            Shift = Shift,
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
            hashCode = hashCode * 397 ^ Code.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    ///   Creates a string for the button to show.
    /// </summary>
    /// <returns>A human-readable string.</returns>
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

        AppendModifierText(toStringBuilder);

        if (Type is InputType.Key or InputType.KeyLabel or InputType.PhysicalKey)
        {
            // If the key is not defined in KeyNames.cs, the string will just be returned unmodified by Translate()

            var key = (Key)Code;

            if (Type == InputType.PhysicalKey)
            {
                key = DisplayServer.KeyboardGetKeycodeFromPhysical(key);
            }

            // Key labels are hopefully already in a format that makes sense when translated

            toStringBuilder.Append(KeyNames.Translate(key));
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

    private void ConstructFrom(InputEventWithModifiers @event, bool preferKeyLabel)
    {
        Control = @event.CtrlPressed || @event.MetaPressed;
        Alt = @event.AltPressed;
        Shift = @event.ShiftPressed;

        switch (@event)
        {
            case InputEventKey inputKey:
            {
                // TODO: unicode key value support?
                if (inputKey.PhysicalKeycode != Key.None && !preferKeyLabel)
                {
                    // Physical key
                    Type = InputType.PhysicalKey;
                    Code = (ulong)inputKey.PhysicalKeycode;
                }
                else if (inputKey.KeyLabel != Key.None)
                {
                    // Key label
                    Type = InputType.KeyLabel;
                    Code = (ulong)inputKey.KeyLabel;
                }
                else
                {
                    // A "normal" key code
                    Type = InputType.Key;
                    Code = (ulong)inputKey.Keycode;
                }

                break;
            }

            case InputEventMouseButton inputMouse:
            {
                Type = InputType.MouseButton;
                Code = (ulong)inputMouse.ButtonIndex;
                break;
            }

            default:
                throw new ArgumentException("Unknown type of event to convert to input key");
        }
    }

    private void AppendModifierText(StringBuilder stringBuilder)
    {
        if (Control)
        {
            stringBuilder.Append(Localization.Translate("CTRL"));
            stringBuilder.Append('+');
        }

        if (Alt)
        {
            stringBuilder.Append(Localization.Translate("ALT"));
            stringBuilder.Append('+');
        }

        if (Shift)
        {
            stringBuilder.Append(Localization.Translate("SHIFT"));
            stringBuilder.Append('+');
        }
    }

    private InputEventWithModifiers ToInputEventWithModifiers()
    {
        InputEventWithModifiers result = Type switch
        {
            InputType.Key => new InputEventKey { Keycode = (Key)Code, Pressed = true },
            InputType.PhysicalKey => new InputEventKey { PhysicalKeycode = (Key)Code, Pressed = true },
            InputType.KeyLabel => new InputEventKey { KeyLabel = (Key)Code, Pressed = true },
            InputType.MouseButton => new InputEventMouseButton { ButtonIndex = (MouseButton)Code, Pressed = true },
            _ => throw new NotSupportedException("Unsupported InputType given"),
        };

        result.AltPressed = Alt;

        // Handle automatic control to meta remapping
        if (FeatureInformation.IsMac())
        {
            result.MetaPressed = Control;
        }
        else
        {
            result.CtrlPressed = Control;
        }

        result.ShiftPressed = Shift;
        return result;
    }

    private TextureRect CreateTextureRect(string image, bool small = false)
    {
        return new TextureRect
        {
            Texture = GD.Load<Texture2D>(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = small ? new Vector2(14, 14) : new Vector2(32, 32),
        };
    }
}
