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
            // ReSharper disable CommentTypo
            // ReSharper disable StringLiteralTypo
            Key.None => Localization.Translate("UNKNOWN"),
            Key.Unknown => Localization.Translate("UNKNOWN"),
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

            // TODO: removed key codes from Godot 4
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

            // New keys with Godot 4

            Key.Escape => Localization.Translate("ESCAPE"),
            Key.Backtab => Localization.Translate("KEY_BACKTAB"),
            Key.Backspace => Localization.Translate("BACKSPACE"),
            Key.KpEnter => Localization.Translate("KPENTER"),
            Key.Sysreq => Localization.Translate("SYSREQ"),
            Key.Pageup => Localization.Translate("PAGEUP"),
            Key.Pagedown => Localization.Translate("PAGEDOWN"),
            Key.Shift => Localization.Translate("SHIFT"),
            Key.Ctrl => Localization.Translate("CTRL"),
            Key.Meta => Localization.Translate("KEY_META"),
            Key.Alt => Localization.Translate("ALT"),
            Key.Capslock => Localization.Translate("CAPSLOCK"),
            Key.Numlock => Localization.Translate("NUMLOCK"),
            Key.Scrolllock => Localization.Translate("SCROLLLOCK"),
            Key.F1 => "F1",
            Key.F2 => "F2",
            Key.F3 => "F3",
            Key.F4 => "F4",
            Key.F5 => "F5",
            Key.F6 => "F6",
            Key.F7 => "F7",
            Key.F8 => "F8",
            Key.F9 => "F9",
            Key.F10 => "F10",
            Key.F11 => "F11",
            Key.F12 => "F12",
            Key.F13 => "F13",
            Key.F14 => "F14",
            Key.F15 => "F15",
            Key.F16 => "F16",
            Key.F17 => "F17",
            Key.F18 => "F18",
            Key.F19 => "F19",
            Key.F20 => "F20",
            Key.F21 => "F21",
            Key.F22 => "F22",
            Key.F23 => "F23",
            Key.F24 => "F24",
            Key.F25 => "F25",
            Key.F26 => "F26",
            Key.F27 => "F27",
            Key.F28 => "F28",
            Key.F29 => "F29",
            Key.F30 => "F30",
            Key.F31 => "F31",
            Key.F32 => "F32",
            Key.F33 => "F33",
            Key.F34 => "F34",
            Key.F35 => "F35",
            Key.KpMultiply => Localization.Translate("KPMULTIPLY"),
            Key.KpDivide => Localization.Translate("KPDIVIDE"),
            Key.KpSubtract => Localization.Translate("KPSUBTRACT"),
            Key.KpPeriod => Localization.Translate("KPPERIOD"),
            Key.KpAdd => Localization.Translate("KPADD"),
            Key.Kp0 => Localization.Translate("KP0"),
            Key.Kp1 => Localization.Translate("KP1"),
            Key.Kp2 => Localization.Translate("KP2"),
            Key.Kp3 => Localization.Translate("KP3"),
            Key.Kp4 => Localization.Translate("KP4"),
            Key.Kp5 => Localization.Translate("KP5"),
            Key.Kp6 => Localization.Translate("KP6"),
            Key.Kp7 => Localization.Translate("KP7"),
            Key.Kp8 => Localization.Translate("KP8"),
            Key.Kp9 => Localization.Translate("KP9"),
            Key.Hyper => Localization.Translate("KEY_HYPER"),
            Key.Volumedown => Localization.Translate("VOLUMEDOWN"),
            Key.Volumemute => Localization.Translate("VOLUMEMUTE"),
            Key.Volumeup => Localization.Translate("VOLUMEUP"),
            Key.Mediaplay => Localization.Translate("MEDIAPLAY"),
            Key.Mediastop => Localization.Translate("MEDIASTOP"),
            Key.Mediaprevious => Localization.Translate("MEDIAPREVIOUS"),
            Key.Medianext => Localization.Translate("MEDIANEXT"),
            Key.Mediarecord => Localization.Translate("MEDIARECORD"),
            Key.Launchmail => Localization.Translate("LAUNCHMAIL"),
            Key.Launchmedia => Localization.Translate("LAUNCHMEDIA"),
            Key.Launch0 => Localization.Translate("LAUNCH0"),
            Key.Launch1 => Localization.Translate("LAUNCH1"),
            Key.Launch2 => Localization.Translate("LAUNCH2"),
            Key.Launch3 => Localization.Translate("LAUNCH3"),
            Key.Launch4 => Localization.Translate("LAUNCH4"),
            Key.Launch5 => Localization.Translate("LAUNCH5"),
            Key.Launch6 => Localization.Translate("LAUNCH6"),
            Key.Launch7 => Localization.Translate("LAUNCH7"),
            Key.Launch8 => Localization.Translate("LAUNCH8"),
            Key.Launch9 => Localization.Translate("LAUNCH9"),
            Key.Launcha => Localization.Translate("LAUNCHA"),
            Key.Launchb => Localization.Translate("LAUNCHB"),
            Key.Launchc => Localization.Translate("LAUNCHC"),
            Key.Launchd => Localization.Translate("LAUNCHD"),
            Key.Launche => Localization.Translate("LAUNCHE"),
            Key.Launchf => Localization.Translate("LAUNCHF"),
            Key.Globe => Localization.Translate("KEY_GLOBE"),
            Key.Keyboard => Localization.Translate("KEY_BRING_UP_KEYBOARD"),
            Key.JisEisu => Localization.Translate("KEY_JIS_EISU"),
            Key.JisKana => Localization.Translate("KEY_JIS_KANA"),
            Key.Space => Localization.Translate("SPACE"),
            Key.A => "A",
            Key.B => "B",
            Key.C => "C",
            Key.D => "D",
            Key.E => "E",
            Key.F => "F",
            Key.G => "G",
            Key.H => "H",
            Key.I => "I",
            Key.J => "J",
            Key.K => "K",
            Key.L => "L",
            Key.M => "M",
            Key.N => "N",
            Key.O => "O",
            Key.P => "P",
            Key.Q => "Q",
            Key.R => "R",
            Key.S => "S",
            Key.T => "T",
            Key.U => "U",
            Key.V => "V",
            Key.W => "W",
            Key.X => "X",
            Key.Y => "Y",
            Key.Z => "Z",
            Key.Backslash => Localization.Translate("BACKSLASH"),

            // Keys that no longer exist:
            /*Localization.Translate("BASSBOOST")
            Localization.Translate("BASSUP")
            Localization.Translate("BASSDOWN")
            Localization.Translate("TREBLEUP")
            Localization.Translate("TREBLEDOWN")
            Localization.Translate("SUPERL")
            Localization.Translate("SUPERR")
            Localization.Translate("DIRECTIONL")
            Localization.Translate("DIRECTIONR")*/

            // ReSharper restore CommentTypo
            // ReSharper restore StringLiteralTypo

            _ => OS.GetKeycodeString(keyCode),

            // Old fallback code:
            // Localization.Translate(keyCode.ToString().ToUpper(CultureInfo.InvariantCulture))
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
}
