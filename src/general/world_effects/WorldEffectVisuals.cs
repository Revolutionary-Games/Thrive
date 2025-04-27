using System.Collections.Generic;

public enum WorldEffectTypes
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
}

public class WorldEffectVisuals
{
    public static readonly Dictionary<WorldEffectTypes, string> EventsIcons = new()
    {
        [WorldEffectTypes.UnderwaterVentEruption] = "res://assets/textures/gui/bevel/Eruption.svg",
        [WorldEffectTypes.GlobalGlaciation] = "res://assets/textures/gui/bevel/GlobalGlaciation.svg",
        [WorldEffectTypes.MeteorPlainImpact] = "res://assets/textures/gui/bevel/MeteorPlain.svg",
        [WorldEffectTypes.MeteorIronImpact] = "res://assets/textures/gui/bevel/MeteorIron.svg",
        [WorldEffectTypes.MeteorPhosphatesImpact] = "res://assets/textures/gui/bevel/MeteorPhosphates.svg",
        [WorldEffectTypes.MeteorRadioactiveImpact] = "res://assets/textures/gui/bevel/MeteorRadioactive.svg",
        [WorldEffectTypes.MeteorGlucoseImpact] = "res://assets/textures/gui/bevel/MeteorGlucose.svg",
        [WorldEffectTypes.MeteorSulfurImpact] = "res://assets/textures/gui/bevel/MeteorSulfur.svg",
    };

    public static readonly Dictionary<WorldEffectTypes, LocalizedString> EventsTooltips = new()
    {
        [WorldEffectTypes.UnderwaterVentEruption] = new LocalizedString("EVENT_ERUPTION_TOOLTIP"),
        [WorldEffectTypes.GlobalGlaciation] = new LocalizedString("GLOBAL_GLACIATION_EVENT_TOOLTIP"),
        [WorldEffectTypes.MeteorPlainImpact] = new LocalizedString("EVENT_METEOR_PLAIN"),
        [WorldEffectTypes.MeteorIronImpact] = new LocalizedString("EVENT_METEOR_IRON"),
        [WorldEffectTypes.MeteorPhosphatesImpact] = new LocalizedString("EVENT_METEOR_PHOSPHATES"),
        [WorldEffectTypes.MeteorRadioactiveImpact] = new LocalizedString("EVENT_METEOR_RADIOACTIVE"),
        [WorldEffectTypes.MeteorGlucoseImpact] = new LocalizedString("EVENT_METEOR_GLUCOSE"),
        [WorldEffectTypes.MeteorSulfurImpact] = new LocalizedString("EVENT_METEOR_SULFUR"),
    };
}
