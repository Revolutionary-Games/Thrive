using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

/// <summary>
///   Access to translated text. Acts as a wrapper around the Godot standard <see cref="TranslationServer"/> to allow
///   caching string name instances to allow text lookup without other code needing to hold on to a bunch of string
///   name objects.
/// </summary>
[GodotAutoload]
public partial class Localization : Node
{
    private static Localization? instance;
    private static bool printedError;

    /// <summary>
    ///   Cache of looked up translations for current language. Will be cleared on language change
    /// </summary>
    private readonly Dictionary<string, string> lookedUpTranslations = new();

    private Localization()
    {
        instance = this;
    }

    public delegate void TranslationsChangedEventHandler();

    /// <summary>
    ///   Event to listen to, to know when translations text / language has changed and some GUI elements may need to
    ///   update their state
    /// </summary>
    public event TranslationsChangedEventHandler? OnTranslationsChanged;

    public static Localization Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   Returns text for a translation key for the current language
    /// </summary>
    /// <param name="message">The translation key to look up, for example <c>"PATCH_NAME"</c></param>
    /// <returns>The text for the key. If the key is invalid will return the key as-is.</returns>
    public static string Translate(string message)
    {
#if DEBUG
        if (message == null!)
        {
            GD.PrintErr("Trying to translate a null string");

            if (Debugger.IsAttached)
                Debugger.Break();

            return string.Empty;
        }
#endif

        if (message == string.Empty)
            return string.Empty;

        var local = instance;

        if (local == null)
        {
            if (!printedError)
            {
                printedError = true;
                GD.PrintErr("Localization is being accessed before it was initialized. This bypasses memory caching.");
            }

            // This works, but does some pretty nasty memory allocations
            return TranslationServer.Translate(message);
        }

        string? result;
        lock (local.lookedUpTranslations)
        {
            if (local.lookedUpTranslations.TryGetValue(message, out result))
                return result;

            // This allocates memory, but as we cache it, this is not so bad
            result = TranslationServer.Translate(message);

            local.lookedUpTranslations.Add(message, result);
        }

        return result;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (instance == this)
            instance = null;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
            OnLanguageChanged();
    }

    private void OnLanguageChanged()
    {
        // Allow multiple threads to try to read translations at once
        lock (lookedUpTranslations)
        {
            lookedUpTranslations.Clear();
        }

        try
        {
            OnTranslationsChanged?.Invoke();
        }
        catch (Exception e)
        {
            GD.PrintErr("Error in some language change callback, not all text will have reacted: ", e);
        }
    }
}
