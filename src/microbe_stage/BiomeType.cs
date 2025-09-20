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
    Coastal, // continental biome
    Estuary, // continental biome
    Tidepool, // continental biome
    Epipelagic, // ocean biome
    Mesopelagic, // ocean biome
    Bathypelagic, // ocean biome
    Abyssopelagic, // ocean biome
    Seafloor, // ocean biome
    Cave,
    IceShelf, // ocean biome
    Vents,
    Banana, // continental biome
}
