/// <summary>
///   Upgrades for toxin firing organelles
/// </summary>
/// <remarks>
///   <para>
///     This is in a separate files as there isn't a toxin organelle component file to put this into
///   </para>
/// </remarks>
[JSONDynamicTypeAllowed]
public class ToxinUpgrades : IComponentSpecificUpgrades
{
    public ToxinUpgrades(ToxinType baseType)
    {
        BaseType = baseType;
    }

    /// <summary>
    ///   Category of the selected toxin to fire. Note that this doesn't *really* need to be here as the toxin type
    ///   is actually determined by the unlocked features in the base upgrades class
    /// </summary>
    public ToxinType BaseType { get; set; }

    // TODO: add this (and add MP cost for modifying this)
    // public float Potency { get; set; }

    public object Clone()
    {
        return new ToxinUpgrades(BaseType);
    }

    public bool Equals(IComponentSpecificUpgrades? other)
    {
        if (other is ToxinUpgrades toxinUpgrades)
        {
            return toxinUpgrades.BaseType == BaseType;
        }

        return false;
    }
}
