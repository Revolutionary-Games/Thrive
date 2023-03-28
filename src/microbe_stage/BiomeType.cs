/// <summary>
///   This contains predefined biome types
/// </summary>
/// <remarks>
///   <para>
///     The order of this enum CANNOT be changed. They are saved in game save files,
///     and re-ordering would break existing saves
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
