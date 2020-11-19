public enum InputType
{
    /// <summary>
    ///   Fires the method once when the key is pressed down
    /// </summary>
    Press,

    /// <summary>
    ///   Fires the method repeatedly when the key is held down
    /// </summary>
    Hold,

    /// <summary>
    ///   Fires the method once when the key is released
    /// </summary>
    Released,

    /// <summary>
    ///   Fires the method repeatedly and toggles when the key is pressed down
    /// </summary>
    ToggleHold,
}
