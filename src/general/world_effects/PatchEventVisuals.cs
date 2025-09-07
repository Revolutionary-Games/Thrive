using System.Collections.Generic;

public enum PatchEventTypes
{
    /// <summary>
    ///   Used as a placeholder value instead of null
    /// </summary>
    None,

    UnderwaterVentEruption,
    GlobalGlaciation,
    MeteorPlainImpact,
    MeteorIronImpact,
    MeteorPhosphatesImpact,
    MeteorRadioactiveImpact,
    MeteorGlucoseImpact,
    MeteorSulfurImpact,
    Runoff,
    Upwelling,
    CurrentDilution,
    TurbidWaters,
    IncreasedStratification,
}

public class PatchEventVisuals
{
    public static readonly Dictionary<PatchEventTypes, string> EventsIcons = new()
    {
        [PatchEventTypes.UnderwaterVentEruption] = "res://assets/textures/gui/bevel/Eruption.svg",
        [PatchEventTypes.GlobalGlaciation] = "res://assets/textures/gui/bevel/GlobalGlaciation.svg",
        [PatchEventTypes.MeteorPlainImpact] = "res://assets/textures/gui/bevel/MeteorPlain.svg",
        [PatchEventTypes.MeteorIronImpact] = "res://assets/textures/gui/bevel/MeteorIron.svg",
        [PatchEventTypes.MeteorPhosphatesImpact] = "res://assets/textures/gui/bevel/MeteorPhosphates.svg",
        [PatchEventTypes.MeteorRadioactiveImpact] = "res://assets/textures/gui/bevel/MeteorRadioactive.svg",
        [PatchEventTypes.MeteorGlucoseImpact] = "res://assets/textures/gui/bevel/MeteorGlucose.svg",
        [PatchEventTypes.MeteorSulfurImpact] = "res://assets/textures/gui/bevel/MeteorSulfur.svg",
        [PatchEventTypes.Runoff] = "res://assets/textures/gui/bevel/RunoffTest.svg",
    };

    public static readonly Dictionary<PatchEventTypes, LocalizedString> EventsTooltips = new()
    {
        [PatchEventTypes.UnderwaterVentEruption] = new LocalizedString("EVENT_ERUPTION_TOOLTIP"),
        [PatchEventTypes.GlobalGlaciation] = new LocalizedString("GLOBAL_GLACIATION_EVENT_TOOLTIP"),
        [PatchEventTypes.MeteorPlainImpact] = new LocalizedString("EVENT_METEOR_PLAIN"),
        [PatchEventTypes.MeteorIronImpact] = new LocalizedString("EVENT_METEOR_IRON"),
        [PatchEventTypes.MeteorPhosphatesImpact] = new LocalizedString("EVENT_METEOR_PHOSPHATES"),
        [PatchEventTypes.MeteorRadioactiveImpact] = new LocalizedString("EVENT_METEOR_RADIOACTIVE"),
        [PatchEventTypes.MeteorGlucoseImpact] = new LocalizedString("EVENT_METEOR_GLUCOSE"),
        [PatchEventTypes.MeteorSulfurImpact] = new LocalizedString("EVENT_METEOR_SULFUR"),
        [PatchEventTypes.Runoff] = new LocalizedString("EVENT_RUNOFF_TOOLTIP"),
    };
}

public class PatchEventProperties
{
    // public float HydrogenSulfideAmountMultiplier = 1.0f;
    // public float HydrogenSulfideDensityMultiplier = 1.0f;
    // public float GlucoseAmountMultiplier = 1.0f;
    // public float GlucoseDensityMultiplier = 1.0f;
    // public float IronAmountMultiplier = 1.0f;
    // public float IronDensityMultiplier = 1.0f;
    // public float AmmoniaAmountMultiplier = 1.0f;
    // public float AmmoniaDensityMultiplier = 1.0f;
    // public float PhosphatesAmountMultiplier = 1.0f;
    // public float PhosphatesDensityMultiplier = 1.0f;
    // public float RadiationAmountMultiplier = 1.0f;
    // public float RadiationDensityMultiplier = 1.0f;

    public float SunlightAmbientMultiplier = 1.0f;
    public float TemperatureAmbientChange = 0.0f;
    public float? TemperatureAmbientFixedValue = null;
    
    public override string ToString()
    {
        return
            // $"HydrogenSulfideAmountMultiplier: {HydrogenSulfideAmountMultiplier}, " +
            // $"HydrogenSulfideDensityMultiplier: {HydrogenSulfideDensityMultiplier}, " +
            // $"GlucoseAmountMultiplier: {GlucoseAmountMultiplier}, " +
            // $"GlucoseDensityMultiplier: {GlucoseDensityMultiplier}, " +
            // $"IronAmountMultiplier: {IronAmountMultiplier}, " +
            // $"IronDensityMultiplier: {IronDensityMultiplier}, " +
            // $"AmmoniaAmountMultiplier: {AmmoniaAmountMultiplier}, " +
            // $"AmmoniaDensityMultiplier: {AmmoniaDensityMultiplier}, " +
            // $"PhosphatesAmountMultiplier: {PhosphatesAmountMultiplier}, " +
            // $"PhosphatesDensityMultiplier: {PhosphatesDensityMultiplier}, " +
            // $"RadiationAmountMultiplier: {RadiationAmountMultiplier}, " +
            // $"RadiationDensityMultiplier: {RadiationDensityMultiplier}, " +
            $"SunlightAmbientMultiplier: {SunlightAmbientMultiplier}, " +
            $"TemperatureAmbientChange: {TemperatureAmbientChange}, " +
            $"TemperatureAmbientFixedValue: {TemperatureAmbientFixedValue}";
    }
}

