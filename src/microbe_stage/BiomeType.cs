/// <summary>
///   This contains predefined biome types
/// </summary>
/// <remarks>
///   <para>
///     The order of this enum CANNOT be changed. They are used as patch IDs in classic map,
///     and are directly used in the patch generation logic.
///   </para>
/// </remarks>
public enum BiomeType
{
    Coastal,
    Estuary,
    Tidepool,
    Epipelagic,
    Mesopelagic,
    Bathypelagic,
    Abyssopelagic,
    Seafloor,
    Cave,
    IceShelf,
    Vents,
}
