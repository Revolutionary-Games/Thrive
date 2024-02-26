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
            Key.Exclam => "¡",
            Key.Cent => "¢",
            Key.Sterling => "£",
            Key.Currency => "¤",
            Key.Yen => "¥",
            Key.Brokenbar => "¦",
            Key.Section => "§",
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
            Key.Ydiaeresis => "ÿ",

            // Key names that would conflict with simple words in translations
            Key.Forward => TranslationServer.Translate("KEY_FORWARD"),
            Key.Tab => TranslationServer.Translate("KEY_TAB"),
            Key.Enter => TranslationServer.Translate("KEY_ENTER"),
            Key.Insert => TranslationServer.Translate("KEY_INSERT"),
            Key.Delete => TranslationServer.Translate("KEY_DELETE"),
            Key.Pause => TranslationServer.Translate("KEY_PAUSE"),
            Key.Clear => TranslationServer.Translate("KEY_CLEAR"),
            Key.Home => TranslationServer.Translate("KEY_HOME"),
            Key.End => TranslationServer.Translate("KEY_END"),
            Key.Left => TranslationServer.Translate("KEY_LEFT"),
            Key.Up => TranslationServer.Translate("KEY_UP"),
            Key.Right => TranslationServer.Translate("KEY_RIGHT"),
            Key.Down => TranslationServer.Translate("KEY_DOWN"),
            Key.Menu => TranslationServer.Translate("KEY_MENU"),
            Key.Help => TranslationServer.Translate("KEY_HELP"),
            Key.Back => TranslationServer.Translate("KEY_BACK"),
            Key.Stop => TranslationServer.Translate("KEY_STOP"),
            Key.Refresh => TranslationServer.Translate("KEY_REFRESH"),
            Key.Search => TranslationServer.Translate("KEY_SEARCH"),
            Key.Standby => TranslationServer.Translate("KEY_STANDBY"),
            Key.Openurl => TranslationServer.Translate("KEY_OPENURL"),
            Key.Homepage => TranslationServer.Translate("KEY_HOMEPAGE"),
            Key.Favorites => TranslationServer.Translate("KEY_FAVORITES"),
            Key.Print => TranslationServer.Translate("KEY_PRINT"),

            // Fallback to using the key name (in upper case) to translate. These must all be defined in Keys method
            _ => TranslationServer.Translate(key.ToString().ToUpper(CultureInfo.InvariantCulture)),
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
        TranslationServer.Translate("SPACE");
        TranslationServer.Translate("BACKSLASH");
        TranslationServer.Translate("ESCAPE");
        TranslationServer.Translate("BACKSPACE");
        TranslationServer.Translate("KPENTER");
        TranslationServer.Translate("SYSREQ");
        TranslationServer.Translate("PAGEUP");
        TranslationServer.Translate("PAGEDOWN");
        TranslationServer.Translate("CAPSLOCK");
        TranslationServer.Translate("NUMLOCK");
        TranslationServer.Translate("SCROLLLOCK");
        TranslationServer.Translate("SUPERL");
        TranslationServer.Translate("SUPERR");
        TranslationServer.Translate("HYPERL");
        TranslationServer.Translate("HYPERR");
        TranslationServer.Translate("DIRECTIONL");
        TranslationServer.Translate("DIRECTIONR");
        TranslationServer.Translate("VOLUMEDOWN");
        TranslationServer.Translate("VOLUMEMUTE");
        TranslationServer.Translate("VOLUMEUP");
        TranslationServer.Translate("BASSBOOST");
        TranslationServer.Translate("BASSUP");
        TranslationServer.Translate("BASSDOWN");
        TranslationServer.Translate("TREBLEUP");
        TranslationServer.Translate("TREBLEDOWN");
        TranslationServer.Translate("MEDIAPLAY");
        TranslationServer.Translate("MEDIASTOP");
        TranslationServer.Translate("MEDIAPREVIOUS");
        TranslationServer.Translate("MEDIANEXT");
        TranslationServer.Translate("MEDIARECORD");
        TranslationServer.Translate("LAUNCHMAIL");
        TranslationServer.Translate("LAUNCHMEDIA");
        TranslationServer.Translate("LAUNCH0");
        TranslationServer.Translate("LAUNCH1");
        TranslationServer.Translate("LAUNCH2");
        TranslationServer.Translate("LAUNCH3");
        TranslationServer.Translate("LAUNCH4");
        TranslationServer.Translate("LAUNCH5");
        TranslationServer.Translate("LAUNCH6");
        TranslationServer.Translate("LAUNCH7");
        TranslationServer.Translate("LAUNCH8");
        TranslationServer.Translate("LAUNCH9");
        TranslationServer.Translate("LAUNCHA");
        TranslationServer.Translate("LAUNCHB");
        TranslationServer.Translate("LAUNCHC");
        TranslationServer.Translate("LAUNCHD");
        TranslationServer.Translate("LAUNCHE");
        TranslationServer.Translate("LAUNCHF");
        TranslationServer.Translate("KPMULTIPLY");
        TranslationServer.Translate("KPDIVIDE");
        TranslationServer.Translate("KPSUBTRACT");
        TranslationServer.Translate("KPPERIOD");
        TranslationServer.Translate("KPADD");
        TranslationServer.Translate("KP0");
        TranslationServer.Translate("KP1");
        TranslationServer.Translate("KP2");
        TranslationServer.Translate("KP3");
        TranslationServer.Translate("KP4");
        TranslationServer.Translate("KP5");
        TranslationServer.Translate("KP6");
        TranslationServer.Translate("KP7");
        TranslationServer.Translate("KP8");
        TranslationServer.Translate("KP9");
        TranslationServer.Translate("UNKNOWN");

        // ReSharper restore StringLiteralTypo
    }
}
