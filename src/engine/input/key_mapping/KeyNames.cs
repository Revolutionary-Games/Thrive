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
    public static string Translate(uint keyCode)
    {
        var key = (KeyList)keyCode;
        return key switch
        {
            KeyList.Exclam => "!",
            KeyList.Quotedbl => "\"",
            KeyList.Numbersign => "#",
            KeyList.Dollar => "$",
            KeyList.Percent => "%",
            KeyList.Ampersand => "&",
            KeyList.Apostrophe => "'",
            KeyList.Parenleft => "(",
            KeyList.Parenright => ")",
            KeyList.Asterisk => "*",
            KeyList.Plus => "+",
            KeyList.Comma => ",",
            KeyList.Minus => "-",
            KeyList.Period => ".",
            KeyList.Slash => "/",
            KeyList.Key0 => "0",
            KeyList.Key1 => "1",
            KeyList.Key2 => "2",
            KeyList.Key3 => "3",
            KeyList.Key4 => "4",
            KeyList.Key5 => "5",
            KeyList.Key6 => "6",
            KeyList.Key7 => "7",
            KeyList.Key8 => "8",
            KeyList.Key9 => "9",
            KeyList.Colon => ":",
            KeyList.Semicolon => ";",
            KeyList.Less => "<",
            KeyList.Equal => "=",
            KeyList.Greater => ">",
            KeyList.Question => "?",
            KeyList.At => "@",
            KeyList.Bracketleft => "[",
            KeyList.Bracketright => "]",
            KeyList.Asciicircum => "^",
            KeyList.Underscore => "_",
            KeyList.Quoteleft => "`",
            KeyList.Braceleft => "{",
            KeyList.Bar => "|",
            KeyList.Braceright => "}",
            KeyList.Asciitilde => "~",
            KeyList.Exclamdown => "¡",
            KeyList.Cent => "¢",
            KeyList.Sterling => "£",
            KeyList.Currency => "¤",
            KeyList.Yen => "¥",
            KeyList.Brokenbar => "¦",
            KeyList.Section => "§",
            KeyList.Diaeresis => "¨",
            KeyList.Copyright => "©",
            KeyList.Ordfeminine => "ª",
            KeyList.Guillemotleft => "«",
            KeyList.Notsign => "¬",
            KeyList.Hyphen => "-",
            KeyList.Registered => "®",
            KeyList.Macron => "¯",
            KeyList.Degree => "°",
            KeyList.Plusminus => "±",
            KeyList.Twosuperior => "²",
            KeyList.Threesuperior => "³",
            KeyList.Acute => "´",
            KeyList.Mu => "µ",
            KeyList.Paragraph => "¶",
            KeyList.Periodcentered => "·",
            KeyList.Cedilla => "¸",
            KeyList.Onesuperior => "¹",
            KeyList.Masculine => "º",
            KeyList.Guillemotright => "»",
            KeyList.Onequarter => "¼",
            KeyList.Onehalf => "½",
            KeyList.Threequarters => "¾",
            KeyList.Questiondown => "¿",
            KeyList.Agrave => "À",
            KeyList.Aacute => "Á",
            KeyList.Acircumflex => "Â",
            KeyList.Atilde => "Ã",
            KeyList.Adiaeresis => "Ä",
            KeyList.Aring => "Å",
            KeyList.Ae => "Æ",
            KeyList.Ccedilla => "Ç",
            KeyList.Egrave => "È",
            KeyList.Eacute => "É",
            KeyList.Ecircumflex => "Ê",
            KeyList.Ediaeresis => "Ë",
            KeyList.Igrave => "Ì",
            KeyList.Iacute => "Í",
            KeyList.Icircumflex => "Î",
            KeyList.Idiaeresis => "Ï",
            KeyList.Eth => "Ð",
            KeyList.Ntilde => "Ñ",
            KeyList.Ograve => "Ò",
            KeyList.Oacute => "Ó",
            KeyList.Ocircumflex => "Ô",
            KeyList.Otilde => "Õ",
            KeyList.Odiaeresis => "Ö",
            KeyList.Multiply => "×",
            KeyList.Ooblique => "Ø",
            KeyList.Ugrave => "Ù",
            KeyList.Uacute => "Ú",
            KeyList.Ucircumflex => "Û",
            KeyList.Udiaeresis => "Ü",
            KeyList.Yacute => "Ý",
            KeyList.Thorn => "Þ",
            KeyList.Ssharp => "ß",
            KeyList.Division => "÷",
            KeyList.Ydiaeresis => "ÿ",

            // Key names that would conflict with simple words in translations
            KeyList.Forward => TranslationServer.Translate("KEY_FORWARD"),
            KeyList.Tab => TranslationServer.Translate("KEY_TAB"),
            KeyList.Enter => TranslationServer.Translate("KEY_ENTER"),
            KeyList.Insert => TranslationServer.Translate("KEY_INSERT"),
            KeyList.Delete => TranslationServer.Translate("KEY_DELETE"),
            KeyList.Pause => TranslationServer.Translate("KEY_PAUSE"),
            KeyList.Clear => TranslationServer.Translate("KEY_CLEAR"),
            KeyList.Home => TranslationServer.Translate("KEY_HOME"),
            KeyList.End => TranslationServer.Translate("KEY_END"),
            KeyList.Left => TranslationServer.Translate("KEY_LEFT"),
            KeyList.Up => TranslationServer.Translate("KEY_UP"),
            KeyList.Right => TranslationServer.Translate("KEY_RIGHT"),
            KeyList.Down => TranslationServer.Translate("KEY_DOWN"),
            KeyList.Menu => TranslationServer.Translate("KEY_MENU"),
            KeyList.Help => TranslationServer.Translate("KEY_HELP"),
            KeyList.Back => TranslationServer.Translate("KEY_BACK"),
            KeyList.Stop => TranslationServer.Translate("KEY_STOP"),
            KeyList.Refresh => TranslationServer.Translate("KEY_REFRESH"),
            KeyList.Search => TranslationServer.Translate("KEY_SEARCH"),
            KeyList.Standby => TranslationServer.Translate("KEY_STANDBY"),
            KeyList.Openurl => TranslationServer.Translate("KEY_OPENURL"),
            KeyList.Homepage => TranslationServer.Translate("KEY_HOMEPAGE"),
            KeyList.Favorites => TranslationServer.Translate("KEY_FAVORITES"),
            KeyList.Print => TranslationServer.Translate("KEY_PRINT"),

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
