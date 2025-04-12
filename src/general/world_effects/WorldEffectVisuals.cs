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
        [WorldEffectTypes.MeteorRadioactiveImpact] = "res://assets/textures/gui/bevel/MeteorRadioavtive.svg",
        [WorldEffectTypes.MeteorGlucoseImpact] = "res://assets/textures/gui/bevel/MeteorGlucose.svg",
        [WorldEffectTypes.MeteorSulfurImpact] = "res://assets/textures/gui/bevel/MeteorSulfur.svg",
    };

    public static readonly Dictionary<WorldEffectTypes, string> EventsTooltips = new()
    {
        [WorldEffectTypes.UnderwaterVentEruption] = Localization.Translate("EVENT_ERUPTION_TOOLTIP"),
        [WorldEffectTypes.GlobalGlaciation] = Localization.Translate("GLOBAL_GLACIATION_EVENT_TOOLTIP"),
        [WorldEffectTypes.MeteorPlainImpact] = Localization.Translate("EVENT_METEOR_PLAIN"),
        [WorldEffectTypes.MeteorIronImpact] = Localization.Translate("EVENT_METEOR_IRON"),
        [WorldEffectTypes.MeteorPhosphatesImpact] = Localization.Translate("EVENT_METEOR_PHOSPHATES"),
        [WorldEffectTypes.MeteorRadioactiveImpact] = Localization.Translate("EVENT_METEOR_RADIOACTIVE"),
        [WorldEffectTypes.MeteorGlucoseImpact] = Localization.Translate("EVENT_METEOR_GLUCOSE"),
        [WorldEffectTypes.MeteorSulfurImpact] = Localization.Translate("EVENT_METEOR_SULFUR"),
    };
}
