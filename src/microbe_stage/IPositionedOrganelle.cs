/// <summary>
///   Organelle type along with position information
/// </summary>
public interface IPositionedOrganelle : IPositionedHex
{
    public OrganelleDefinition Definition { get; }

    /// <summary>
    ///   This is now the number of times to rotate. This used to be the angle in degrees
    /// </summary>
    public int Orientation { get; }
}
