/// <summary>
///   Organelle type along with position information
/// </summary>
public interface IPositionedOrganelle : IPositionedHex, IReadOnlyPositionedOrganelle
{
    public OrganelleUpgrades? ModifiableUpgrades { get; }

    public new bool IsEndosymbiont { get; set; }
}

public interface IReadOnlyPositionedOrganelle : IReadOnlyPositionedHex
{
    public OrganelleDefinition Definition { get; }

    /// <summary>
    ///   The upgrades that this organelle has which affect how the components function
    /// </summary>
    public IReadOnlyOrganelleUpgrades? Upgrades { get; }

    /// <summary>
    ///   True if this organelle is an endosymbiont, which makes most operations free.
    /// </summary>
    public bool IsEndosymbiont { get; }
}
