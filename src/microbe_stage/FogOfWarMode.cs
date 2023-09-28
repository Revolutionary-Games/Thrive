/// <summary>
///   Whether the fog of war should be disabled,
///   mark patches adjacent to the player as explored,
///   or mark only the patch the player has entered as explored.
/// </summary>
/// <remarks>
///   <para>
///     Do not modify the order of the elements, as that would break the UI functionality
///   </para>
/// </remarks>
public enum FogOfWarMode
{
    Disabled,
    Normal,
    Intense,
}
