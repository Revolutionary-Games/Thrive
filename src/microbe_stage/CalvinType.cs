using System.ComponentModel;

/// <summary>
///   Calvin cycle types. See <see cref="CalvinUpgradeNames"/> for what names match each enum value in the upgrade list.
/// </summary>
/// <remarks>
///   <para>
///     Don't reorder the values here as that will cause save compatibility to break
///   </para>
/// </remarks>
public enum CalvinType
{
    /// <summary>
    ///   Create Glucose passively.
    /// </summary>
    [Description("TOXIN_OXYTOXY_DESCRIPTION")]
    Glucose = 0,

    /// <summary>
    ///   No Calvin cycle, just create ATP.
    /// </summary>
    [Description("TOXIN_CYTOTOXIN_DESCRIPTION")]
    
    NoCalvin,

    /// <summary>
    ///   Starch creation (not implemented)
    /// </summary>
    
    
    // Starch not yet supported

    /*
    [Description("TOXIN_MACROLIDE_DESCRIPTION")]
    Starch,
    */
}
