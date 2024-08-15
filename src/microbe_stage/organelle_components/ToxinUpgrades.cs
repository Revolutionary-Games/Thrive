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
    public ToxinUpgrades(ToxinType baseType, float toxicity)
    {
        BaseType = baseType;
        Toxicity = toxicity;
    }

    /// <summary>
    ///   Category of the selected toxin to fire. Note that this doesn't *really* need to be here as the toxin type
    ///   is actually determined by the unlocked features in the base upgrades class, but for now this is here for
    ///   completeness’s sake. It is hopefully not possible for this to get out of sync with the other data.
    /// </summary>
    public ToxinType BaseType { get; set; }

    /// <summary>
    ///   Toxicity / speed of firing of the toxin. Range is -1 to 1, with 0 being the default. 1 is maximum potency
    ///   and -1 is maximum firerate with minimum potency.
    /// </summary>
    public float Toxicity { get; set; }

    public bool Equals(IComponentSpecificUpgrades? other)
    {
        if (other is ToxinUpgrades toxinUpgrades)
        {
            return toxinUpgrades.BaseType == BaseType;
        }

        return false;
    }

    public object Clone()
    {
        return new ToxinUpgrades(BaseType, Toxicity);
    }
}
