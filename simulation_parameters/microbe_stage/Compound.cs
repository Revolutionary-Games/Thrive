/// <summary>
///   Specifies a compound type. This is an enum to allow code to be statically made to refer to specific compound
///   types. Actual compound properties are held by <see cref="CompoundDefinition"/> and can be retrieved from
///   <see cref="SimulationParameters"/>.
/// </summary>
/// <remarks>
///   <para>
///     If any new values are added here, they must be added after the existing values (and no values should ever be
///     deleted) but before <see cref="LastInbuiltCompound"/> entry (and that entry needs to be updated to match the
///     new last value).
///   </para>
///   <para>
///     This is defined as being size of 16 bits to be more space efficient when processing a lot of compound data.
///   </para>
/// </remarks>
public enum Compound : ushort
{
    /// <summary>
    ///   Special value reserved to denote, no compound type (this allows compound searches to return a non-nullable
    ///   value 0)
    /// </summary>
    Invalid = 0,

    /// <summary>
    ///   ATP compound. This *must be kept* as the value 1 and here at the start.
    /// </summary>
    ATP = 1,

    Ammonia = 2,
    Phosphate = 3,
    Hydrogensulfide = 4,
    Glucose = 5,
    Oxytoxy = 6,
    Mucilage = 7,
    Iron = 8,
    Oxygen = 9,
    Carbondioxide = 10,
    Nitrogen = 11,
    Sunlight = 12,
    Temperature = 13,

    /// <summary>
    ///   Last defined compound. When adding new values this *must be* updated to match the value of the last compound.
    /// </summary>
    LastInbuiltCompound = 13,

    // This should be plenty for us to implement anything, and leaves a lot of space for mods to load custom compound
    // types into
    MaxInbuiltCompound = 8096,

    /// <summary>
    ///   Mod compounds that aren't defined here start at this value
    /// </summary>
    FirstCustomCompound = 8097,

    // Due to the used underlying type, this is the max value
    MaxCompoundType = 65535,
}
