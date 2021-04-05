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
            _ => TranslationServer.Translate(key.ToString().ToUpper(CultureInfo.InvariantCulture)),
        };
    }

    // ReSharper disable once UnusedMember.Local
    /// <summary>
    ///   Useless method that only exists to tell the translation system specific strings
    /// </summary>
    private static void Keys()
    {
        TranslationServer.Translate("SPACE");
        TranslationServer.Translate("BACKSLASH");
        TranslationServer.Translate("ESCAPE");
        TranslationServer.Translate("TAB");
        TranslationServer.Translate("BACKSPACE");
        TranslationServer.Translate("ENTER");
        TranslationServer.Translate("KPENTER");
        TranslationServer.Translate("INSERT");
        TranslationServer.Translate("DELETE");
        TranslationServer.Translate("PAUSE");
        TranslationServer.Translate("PRINT");
        TranslationServer.Translate("SYSREQ");
        TranslationServer.Translate("CLEAR");
        TranslationServer.Translate("HOME");
        TranslationServer.Translate("END");
        TranslationServer.Translate("LEFT");
        TranslationServer.Translate("UP");
        TranslationServer.Translate("RIGHT");
        TranslationServer.Translate("DOWN");
        TranslationServer.Translate("PAGEUP");
        TranslationServer.Translate("PAGEDOWN");
        TranslationServer.Translate("CAPSLOCK");
        TranslationServer.Translate("NUMLOCK");
        TranslationServer.Translate("SCROLLLOCK");
        TranslationServer.Translate("SUPERL");
        TranslationServer.Translate("SUPERR");
        TranslationServer.Translate("MENU");
        TranslationServer.Translate("HYPERL");
        TranslationServer.Translate("HYPERR");
        TranslationServer.Translate("HELP");
        TranslationServer.Translate("DIRECTIONL");
        TranslationServer.Translate("DIRECTIONR");
        TranslationServer.Translate("BACK");
        TranslationServer.Translate("FORWARD");
        TranslationServer.Translate("STOP");
        TranslationServer.Translate("REFRESH");
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
        TranslationServer.Translate("HOMEPAGE");
        TranslationServer.Translate("FAVORITES");
        TranslationServer.Translate("SEARCH");
        TranslationServer.Translate("STANDBY");
        TranslationServer.Translate("OPENURL");
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
    }
}
