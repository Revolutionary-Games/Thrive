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
        [PatchEventTypes.Runoff] = "res://assets/textures/gui/bevel/Runoff.svg",
        [PatchEventTypes.Upwelling] = "res://assets/textures/gui/bevel/Upwelling.svg",
        [PatchEventTypes.CurrentDilution] = "res://assets/textures/gui/bevel/CurrentDilution.svg",
    };

    public static readonly Dictionary<PatchEventTypes, LocalizedString> EventsDefaultTooltips = new()
    {
        [PatchEventTypes.UnderwaterVentEruption] = new LocalizedString("EVENT_ERUPTION_TOOLTIP"),
        [PatchEventTypes.GlobalGlaciation] = new LocalizedString("GLOBAL_GLACIATION_EVENT_TOOLTIP"),
        [PatchEventTypes.MeteorPlainImpact] = new LocalizedString("EVENT_METEOR_PLAIN"),
        [PatchEventTypes.MeteorIronImpact] = new LocalizedString("EVENT_METEOR_IRON"),
        [PatchEventTypes.MeteorPhosphatesImpact] = new LocalizedString("EVENT_METEOR_PHOSPHATES"),
        [PatchEventTypes.MeteorRadioactiveImpact] = new LocalizedString("EVENT_METEOR_RADIOACTIVE"),
        [PatchEventTypes.MeteorGlucoseImpact] = new LocalizedString("EVENT_METEOR_GLUCOSE"),
        [PatchEventTypes.MeteorSulfurImpact] = new LocalizedString("EVENT_METEOR_SULFUR"),
    };
}

public class PatchEventProperties
{
    public string CustomTooltip = string.Empty;
    public float SunlightAmbientMultiplier = 1.0f;
    public float TemperatureAmbientChange = 0.0f;
    public float? TemperatureAmbientFixedValue = null;

    public override string ToString()
    {
        return
            $"SunlightAmbientMultiplier: {SunlightAmbientMultiplier}, " +
            $"TemperatureAmbientChange: {TemperatureAmbientChange}, " +
            $"TemperatureAmbientFixedValue: {TemperatureAmbientFixedValue}";
    }
}
