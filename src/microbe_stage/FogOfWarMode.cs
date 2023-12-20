using System.ComponentModel;

public enum FogOfWarMode
{
    [Description("FOG_OF_WAR_DISABLED")]
    Ignored = 0,

    [Description("FOG_OF_WAR_REGULAR")]
    Regular = 1,

    [Description("FOG_OF_WAR_INTENSE")]
    Intense = 2,
}
