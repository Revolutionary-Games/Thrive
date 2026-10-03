using System.ComponentModel;

/// <summary>
///   Which gamete type is used by something
/// </summary>
public enum GameteType
{
    /// <summary>
    ///   Used when not specified / need to match all
    /// </summary>
    [Description("GAMETE_TYPE_ALL")]
    All,

    [Description("GAMETE_TYPE_A")]
    A,

    [Description("GAMETE_TYPE_B")]
    B,
}
