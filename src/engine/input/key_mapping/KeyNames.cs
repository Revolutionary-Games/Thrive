// NO_DUPLICATE_CHECK

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
