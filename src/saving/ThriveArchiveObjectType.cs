using SharedBase.Archive;

/// <summary>
///   Thrive-specific types that archives can contain. Values may never be removed or reordered!
/// </summary>
public enum ThriveArchiveObjectType : uint
{
    InvalidThrive = ArchiveObjectType.StartOfCustomTypes,

    Vector3,
    Vector4,
    Quaternion,

    Save,
    SaveInformation,
    GameProperties,
    MicrobeStage,
    TutorialState,
    GameWorld,
    WorldGenerationSettings,
    WorldStatsTracker,
    SimpleStatistic,
    DamageStatistic,
    ReproductionStatistic,
    UnlockProgress,
    DayNightCycle,
    ReproductionOrganelleData,
    TimedWorldOperations,
    GlobalGlaciationEvent,
    MeteorImpactEvent,
    UnderwaterVentEruptionEffect,
    Meteor,
    GlucoseReductionEffect,
    CompoundDiffusionEffect,
    HydrogenSulfideConsumptionEffect,
    NitrogenControlEffect,
    IronOxidationEffect,
    MarineSnowDensityEffect,
    PhotosynthesisProductionEffect,
    VolcanismEffect,
    PatchMap,
}
