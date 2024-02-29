using System.Globalization;
using Godot;

internal static class KeyNames
{
    /// <summary>
    ///   Translates a KeyCode to a printable string
    /// </summary>
    /// <param name="keyCode">The keyCode to translate</param>
    /// <returns>A human readable string</returns>
    public static string Translate(Key keyCode)
    {
        return keyCode switch
        {
            Key.Exclam => "!",
            Key.Quotedbl => "\"",
            Key.Numbersign => "#",
            Key.Dollar => "$",
            Key.Percent => "%",
            Key.Ampersand => "&",
            Key.Apostrophe => "'",
            Key.Parenleft => "(",
            Key.Parenright => ")",
            Key.Asterisk => "*",
            Key.Plus => "+",
            Key.Comma => ",",
            Key.Minus => "-",
            Key.Period => ".",
            Key.Slash => "/",
            Key.Key0 => "0",
            Key.Key1 => "1",
            Key.Key2 => "2",
            Key.Key3 => "3",
            Key.Key4 => "4",
            Key.Key5 => "5",
            Key.Key6 => "6",
            Key.Key7 => "7",
            Key.Key8 => "8",
            Key.Key9 => "9",
            Key.Colon => ":",
            Key.Semicolon => ";",
            Key.Less => "<",
            Key.Equal => "=",
            Key.Greater => ">",
            Key.Question => "?",
            Key.At => "@",
            Key.Bracketleft => "[",
            Key.Bracketright => "]",
            Key.Asciicircum => "^",
            Key.Underscore => "_",
            Key.Quoteleft => "`",
            Key.Braceleft => "{",
            Key.Bar => "|",
            Key.Braceright => "}",
            Key.Asciitilde => "~",
            Key.Yen => "¥",
            Key.Section => "§",

            // TODO: removed keys figure out what to do
            /*Key.Exclam => "¡",
            Key.Cent => "¢",
            Key.Sterling => "£",
            Key.Currency => "¤",
            Key.Brokenbar => "¦",
            Key.Diaeresis => "¨",
            Key.Copyright => "©",
            Key.Ordfeminine => "ª",
            Key.Guillemotleft => "«",
            Key.Notsign => "¬",
            Key.Hyphen => "-",
            Key.Registered => "®",
            Key.Macron => "¯",
            Key.Degree => "°",
            Key.Plusminus => "±",
            Key.Twosuperior => "²",
            Key.Threesuperior => "³",
            Key.Acute => "´",
            Key.Mu => "µ",
            Key.Paragraph => "¶",
            Key.Periodcentered => "·",
            Key.Cedilla => "¸",
            Key.Onesuperior => "¹",
            Key.Masculine => "º",
            Key.Guillemotright => "»",
            Key.Onequarter => "¼",
            Key.Onehalf => "½",
            Key.Threequarters => "¾",
            Key.Questiondown => "¿",
            Key.Agrave => "À",
            Key.Aacute => "Á",
            Key.Acircumflex => "Â",
            Key.Atilde => "Ã",
            Key.Adiaeresis => "Ä",
            Key.Aring => "Å",
            Key.Ae => "Æ",
            Key.Ccedilla => "Ç",
            Key.Egrave => "È",
            Key.Eacute => "É",
            Key.Ecircumflex => "Ê",
            Key.Ediaeresis => "Ë",
            Key.Igrave => "Ì",
            Key.Iacute => "Í",
            Key.Icircumflex => "Î",
            Key.Idiaeresis => "Ï",
            Key.Eth => "Ð",
            Key.Ntilde => "Ñ",
            Key.Ograve => "Ò",
            Key.Oacute => "Ó",
            Key.Ocircumflex => "Ô",
            Key.Otilde => "Õ",
            Key.Odiaeresis => "Ö",
            Key.Multiply => "×",
            Key.Ooblique => "Ø",
            Key.Ugrave => "Ù",
            Key.Uacute => "Ú",
            Key.Ucircumflex => "Û",
            Key.Udiaeresis => "Ü",
            Key.Yacute => "Ý",
            Key.Thorn => "Þ",
            Key.Ssharp => "ß",
            Key.Division => "÷",
            Key.Ydiaeresis => "ÿ",*/

            // Key names that would conflict with simple words in translations
            Key.Forward => Localization.Translate("KEY_FORWARD"),
            Key.Tab => Localization.Translate("KEY_TAB"),
            Key.Enter => Localization.Translate("KEY_ENTER"),
            Key.Insert => Localization.Translate("KEY_INSERT"),
            Key.Delete => Localization.Translate("KEY_DELETE"),
            Key.Pause => Localization.Translate("KEY_PAUSE"),
            Key.Clear => Localization.Translate("KEY_CLEAR"),
            Key.Home => Localization.Translate("KEY_HOME"),
            Key.End => Localization.Translate("KEY_END"),
            Key.Left => Localization.Translate("KEY_LEFT"),
            Key.Up => Localization.Translate("KEY_UP"),
            Key.Right => Localization.Translate("KEY_RIGHT"),
            Key.Down => Localization.Translate("KEY_DOWN"),
            Key.Menu => Localization.Translate("KEY_MENU"),
            Key.Help => Localization.Translate("KEY_HELP"),
            Key.Back => Localization.Translate("KEY_BACK"),
            Key.Stop => Localization.Translate("KEY_STOP"),
            Key.Refresh => Localization.Translate("KEY_REFRESH"),
            Key.Search => Localization.Translate("KEY_SEARCH"),
            Key.Standby => Localization.Translate("KEY_STANDBY"),
            Key.Openurl => Localization.Translate("KEY_OPENURL"),
            Key.Homepage => Localization.Translate("KEY_HOMEPAGE"),
            Key.Favorites => Localization.Translate("KEY_FAVORITES"),
            Key.Print => Localization.Translate("KEY_PRINT"),

            // Fallback to using the key name (in upper case) to translate. These must all be defined in Keys method
            _ => Localization.Translate(keyCode.ToString().ToUpper(CultureInfo.InvariantCulture)),
        };
    }

    /// <summary>
    ///   Translates a controller axis to user readable name
    /// </summary>
    /// <param name="axis">Axis to translate</param>
    /// <param name="direction">Direction of the axis</param>
    /// <param name="activeControllerType">Selects which controller type specific names are used</param>
    /// <returns>The translated string referring to the axis</returns>
    public static string TranslateAxis(JoyAxis axis, float direction, ControllerType activeControllerType)
    {
        string directionString;

        if (direction < 0)
        {
            directionString = Localization.Translate("CONTROLLER_AXIS_NEGATIVE_DIRECTION");
        }
        else
        {
            directionString = Localization.Translate("CONTROLLER_AXIS_POSITIVE_DIRECTION");
        }

        string axisString;
        switch (axis)
        {
            case JoyAxis.LeftX:
                axisString = Localization.Translate("CONTROLLER_AXIS_LEFT_X");
                break;
            case JoyAxis.LeftY:
                axisString = Localization.Translate("CONTROLLER_AXIS_LEFT_Y");
                break;
            case JoyAxis.RightX:
                axisString = Localization.Translate("CONTROLLER_AXIS_RIGHT_X");
                break;
            case JoyAxis.RightY:
                axisString = Localization.Translate("CONTROLLER_AXIS_RIGHT_Y");
                break;

            // Triggers have different names for different controller types so these don't set the axis string like
            // normal as triggers should only be able to go one direction on their axis.
            case JoyAxis.TriggerLeft:
                switch (activeControllerType)
                {
                    case ControllerType.PlayStation3:
                    case ControllerType.PlayStation4:
                    case ControllerType.PlayStation5:
                        return Localization.Translate("CONTROLLER_AXIS_L2");
                    case ControllerType.Xbox360:
                    case ControllerType.XboxOne:
                    case ControllerType.XboxSeriesX:
                    default:
                        return Localization.Translate("CONTROLLER_AXIS_LEFT_TRIGGER");
                }

            case JoyAxis.TriggerRight:
                switch (activeControllerType)
                {
                    case ControllerType.PlayStation3:
                    case ControllerType.PlayStation4:
                    case ControllerType.PlayStation5:
                        return Localization.Translate("CONTROLLER_AXIS_R2");
                    case ControllerType.Xbox360:
                    case ControllerType.XboxOne:
                    case ControllerType.XboxSeriesX:
                    default:
                        return Localization.Translate("CONTROLLER_AXIS_RIGHT_TRIGGER");
                }

            case JoyAxis.Invalid:
            case JoyAxis.SdlMax:
            case JoyAxis.Max:
            default:
                return Localization.Translate("CONTROLLER_UNKNOWN_AXIS");
        }

        return $"{directionString} {axisString}";
    }

    /// <summary>
    ///   Translates a controller button to user readable name
    /// </summary>
    /// <param name="button">Button to translate to a string</param>
    /// <param name="activeControllerType">Selects which controller type specific names are used</param>
    /// <returns>The translated string referring to the axis</returns>
    public static string TranslateControllerButton(JoyButton button, ControllerType activeControllerType)
    {
        switch (button)
        {
            case JoyButton.Invalid:
            case JoyButton.SdlMax:
            case JoyButton.Max:
                return Localization.Translate("CONTROLLER_BUTTON_UNKNOWN");

            case JoyButton.Misc1:
                return Localization.Translate("CONTROLLER_BUTTON_MISC1");
            case JoyButton.Paddle1:
                return Localization.Translate("CONTROLLER_BUTTON_PADDLE1");
            case JoyButton.Paddle2:
                return Localization.Translate("CONTROLLER_BUTTON_PADDLE2");
            case JoyButton.Paddle3:
                return Localization.Translate("CONTROLLER_BUTTON_PADDLE3");
            case JoyButton.Paddle4:
                return Localization.Translate("CONTROLLER_BUTTON_PADDLE4");
            case JoyButton.Touchpad:
                return Localization.Translate("CONTROLLER_BUTTON_TOUCH_PAD");

            case JoyButton.DpadUp:
                return Localization.Translate("CONTROLLER_BUTTON_DPAD_UP");
            case JoyButton.DpadDown:
                return Localization.Translate("CONTROLLER_BUTTON_DPAD_DOWN");
            case JoyButton.DpadLeft:
                return Localization.Translate("CONTROLLER_BUTTON_DPAD_LEFT");
            case JoyButton.DpadRight:
                return Localization.Translate("CONTROLLER_BUTTON_DPAD_RIGHT");
        }

        switch (activeControllerType)
        {
            case ControllerType.PlayStation3:
            {
                // PS3 controller had some different names for buttons than newer ones
                if (button == JoyButton.Back)
                    return Localization.Translate("CONTROLLER_BUTTON_PS3_SELECT");
                if (button == JoyButton.Start)
                    return Localization.Translate("CONTROLLER_BUTTON_PS3_START");

                goto case ControllerType.PlayStation4;
            }

            case ControllerType.PlayStation4:
            case ControllerType.PlayStation5:
                switch (button)
                {
                    case JoyButton.A:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_CROSS");
                    case JoyButton.B:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_CIRCLE");
                    case JoyButton.X:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_SQUARE");
                    case JoyButton.Y:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_TRIANGLE");
                    case JoyButton.Back:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_SHARE");
                    case JoyButton.Guide:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_SONY_BUTTON");
                    case JoyButton.Start:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_OPTIONS");
                    case JoyButton.LeftStick:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_L3");
                    case JoyButton.RightStick:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_R3");
                    case JoyButton.LeftShoulder:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_L1");
                    case JoyButton.RightShoulder:
                        return Localization.Translate("CONTROLLER_BUTTON_PS_R1");
                    default:
                        return Localization.Translate("CONTROLLER_BUTTON_UNKNOWN");
                }

            case ControllerType.Xbox360:
            case ControllerType.XboxOne:
            case ControllerType.XboxSeriesX:
            default:
                switch (button)
                {
                    case JoyButton.A:
                        return Localization.Translate("CONTROLLER_BUTTON_XBOX_A");
                    case JoyButton.B:
                        return Localization.Translate("CONTROLLER_BUTTON_XBOX_B");
                    case JoyButton.X:
                        return Localization.Translate("CONTROLLER_BUTTON_XBOX_X");
                    case JoyButton.Y:
                        return Localization.Translate("CONTROLLER_BUTTON_XBOX_Y");
                    case JoyButton.Back:
                        return Localization.Translate("CONTROLLER_BUTTON_XBOX_BACK");
                    case JoyButton.Guide:
                        return Localization.Translate("CONTROLLER_BUTTON_XBOX_GUIDE");
                    case JoyButton.Start:
                        return Localization.Translate("CONTROLLER_BUTTON_XBOX_START");
                    case JoyButton.LeftStick:
                        return Localization.Translate("CONTROLLER_BUTTON_LEFT_STICK");
                    case JoyButton.RightStick:
                        return Localization.Translate("CONTROLLER_BUTTON_RIGHT_STICK");
                    case JoyButton.LeftShoulder:
                        return Localization.Translate("CONTROLLER_BUTTON_LEFT_SHOULDER");
                    case JoyButton.RightShoulder:
                        return Localization.Translate("CONTROLLER_BUTTON_RIGHT_SHOULDER");
                    default:
                        return Localization.Translate("CONTROLLER_BUTTON_UNKNOWN");
                }
        }
    }

    // ReSharper disable once UnusedMember.Local
    /// <summary>
    ///   Useless method that only exists to tell the translation system specific strings
    /// </summary>
    private static void Keys()
    {
        // Names are from Godot so we need to have these as-is
        // ReSharper disable StringLiteralTypo
        Localization.Translate("SPACE");
        Localization.Translate("BACKSLASH");
        Localization.Translate("ESCAPE");
        Localization.Translate("BACKSPACE");
        Localization.Translate("KPENTER");
        Localization.Translate("SYSREQ");
        Localization.Translate("PAGEUP");
        Localization.Translate("PAGEDOWN");
        Localization.Translate("CAPSLOCK");
        Localization.Translate("NUMLOCK");
        Localization.Translate("SCROLLLOCK");
        Localization.Translate("SUPERL");
        Localization.Translate("SUPERR");
        Localization.Translate("HYPERL");
        Localization.Translate("HYPERR");
        Localization.Translate("DIRECTIONL");
        Localization.Translate("DIRECTIONR");
        Localization.Translate("VOLUMEDOWN");
        Localization.Translate("VOLUMEMUTE");
        Localization.Translate("VOLUMEUP");
        Localization.Translate("BASSBOOST");
        Localization.Translate("BASSUP");
        Localization.Translate("BASSDOWN");
        Localization.Translate("TREBLEUP");
        Localization.Translate("TREBLEDOWN");
        Localization.Translate("MEDIAPLAY");
        Localization.Translate("MEDIASTOP");
        Localization.Translate("MEDIAPREVIOUS");
        Localization.Translate("MEDIANEXT");
        Localization.Translate("MEDIARECORD");
        Localization.Translate("LAUNCHMAIL");
        Localization.Translate("LAUNCHMEDIA");
        Localization.Translate("LAUNCH0");
        Localization.Translate("LAUNCH1");
        Localization.Translate("LAUNCH2");
        Localization.Translate("LAUNCH3");
        Localization.Translate("LAUNCH4");
        Localization.Translate("LAUNCH5");
        Localization.Translate("LAUNCH6");
        Localization.Translate("LAUNCH7");
        Localization.Translate("LAUNCH8");
        Localization.Translate("LAUNCH9");
        Localization.Translate("LAUNCHA");
        Localization.Translate("LAUNCHB");
        Localization.Translate("LAUNCHC");
        Localization.Translate("LAUNCHD");
        Localization.Translate("LAUNCHE");
        Localization.Translate("LAUNCHF");
        Localization.Translate("KPMULTIPLY");
        Localization.Translate("KPDIVIDE");
        Localization.Translate("KPSUBTRACT");
        Localization.Translate("KPPERIOD");
        Localization.Translate("KPADD");
        Localization.Translate("KP0");
        Localization.Translate("KP1");
        Localization.Translate("KP2");
        Localization.Translate("KP3");
        Localization.Translate("KP4");
        Localization.Translate("KP5");
        Localization.Translate("KP6");
        Localization.Translate("KP7");
        Localization.Translate("KP8");
        Localization.Translate("KP9");
        Localization.Translate("UNKNOWN");

        // ReSharper restore StringLiteralTypo
    }
}
