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
    ///   The upgrades that this organelle has which affect how the components function
    /// </summary>
    public IReadOnlyOrganelleUpgrades? Upgrades { get; }
}
