/// <summary>
///   All the states elements on the <see cref="PatchMap"/> can be in when FogOfWar is used
/// </summary>
/// <remarks>
///   <para>
///     Elements HAVE to be stored in this order, with highter int value elements being more hidden
///   </para>
/// </remarks>
public enum MapElementVisibility
{
    Shown = 0,

    Unknown = 1,

    Hidden = 2,
}
