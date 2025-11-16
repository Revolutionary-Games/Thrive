/// <summary>
///   Organelle type along with position information
/// </summary>
public interface IPositionedOrganelle : IPositionedHex, IReadOnlyPositionedOrganelle
{
    public new int Orientation { get; set; }

    public OrganelleUpgrades? ModifiableUpgrades { get; }
}

public interface IReadOnlyPositionedOrganelle : IReadOnlyPositionedHex
{
    public OrganelleDefinition Definition { get; }

    /// <summary>
    ///   This is now the number of times to rotate. This used to be the angle in degrees
    /// </summary>
    public int Orientation { get; }

    /// <summary>
    ///   The upgrades that this organelle has which affect how the components function
    /// </summary>
    public IReadOnlyOrganelleUpgrades? Upgrades { get; }
}
