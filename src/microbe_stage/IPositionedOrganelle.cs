/// <summary>
///   Organelle type along with position information
/// </summary>
public interface IPositionedOrganelle
{
    OrganelleDefinition Definition { get; }
    Hex Position { get; }

    /// <summary>
    ///   This is now the number of times to rotate. This used to be the angle in degrees
    /// </summary>
    int Orientation { get; }
}
