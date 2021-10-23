/// <summary>
///   Class that stores species-wide information.
/// </summary>
/// <remarks>
///   <para>
///     It can be expanded to have any species parameter we might want to keep,
///     e.g. base structure, behavioural values... and such to draw species history.
///     Note that specificities of individuals, such as duplicated organelles, should not be stored here.
///   </para>
/// </remarks>
public class SpeciesInfo
{
    public uint ID;
    public long Population;
}
