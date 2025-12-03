using System;
using Godot;

/// <summary>
///   Helpers related to graphics presets
/// </summary>
public static class GraphicsPresets
{
    /// <summary>
    ///   Preset configurations. Need to be in the same order as the Preset enum. It's unnecessary right now to load
    ///   these from files, but that could in theory be added.
    /// </summary>
    private static readonly PresetConfiguration[] Presets =
    [
        new(),

        // Very low preset
        new()
        {
            AntiAliasingMode = Settings.AntiAliasingMode.Disabled,
            AnisotropicFilterLevel = Viewport.AnisotropicFiltering.Disabled,
            RenderScale = 0.5f,
            UpscalingMethod = Settings.UpscalingMode.Bilinear,
            ChromaticEnabled = false,
            DisplayBackgroundParticles = false,
            MicrobeDistortionStrength = 0,
            MicrobeBackgroundBlurLowQuality = true,
            MicrobeBackgroundBlurStrength = 0,
            MicrobeRippleEffect = false,
            GUILightEffectsEnabled = false,
            BloomEnabled = false,
            Menu3DBackgroundEnabled = false,
        },

        // Low preset
        new()
        {
            AntiAliasingMode = Settings.AntiAliasingMode.Disabled,
            AnisotropicFilterLevel = Viewport.AnisotropicFiltering.Anisotropy2X,
            RenderScale = 0.75f,

            // TODO: this is a key question whether this should be fsr1 or bilinear
            // UpscalingMethod = Settings.UpscalingMode.Fsr1,
            UpscalingMethod = Settings.UpscalingMode.Bilinear,
            ChromaticEnabled = false,
            DisplayBackgroundParticles = false,
            MicrobeDistortionStrength = 0,
            MicrobeBackgroundBlurLowQuality = true,
            MicrobeBackgroundBlurStrength = 0,
            MicrobeRippleEffect = false,
            GUILightEffectsEnabled = true,
            BloomEnabled = false,
            Menu3DBackgroundEnabled = false,
        },

        // Medium preset
        new()
        {
            AntiAliasingMode = Settings.AntiAliasingMode.ScreenSpaceFx,
            AnisotropicFilterLevel = Viewport.AnisotropicFiltering.Anisotropy4X,
            RenderScale = 1,
            UpscalingMethod = Settings.UpscalingMode.Fsr1,
            ChromaticEnabled = true,
            DisplayBackgroundParticles = true,
            MicrobeDistortionStrength = 0.6f,
            MicrobeBackgroundBlurLowQuality = true,
            MicrobeBackgroundBlurStrength = 2,
            MicrobeRippleEffect = false,
            GUILightEffectsEnabled = true,
            BloomEnabled = true,
            Menu3DBackgroundEnabled = true,
        },

        // High preset
        new()
        {
            AntiAliasingMode = Settings.AntiAliasingMode.MSAA,
            MSAAResolution = Viewport.Msaa.Msaa4X,
            AnisotropicFilterLevel = Viewport.AnisotropicFiltering.Anisotropy8X,
            RenderScale = 1,
            UpscalingMethod = Settings.UpscalingMode.Fsr2,
            ChromaticEnabled = true,
            DisplayBackgroundParticles = true,
            MicrobeDistortionStrength = 0.6f,
            MicrobeBackgroundBlurLowQuality = false,
            MicrobeBackgroundBlurStrength = 2,
            MicrobeRippleEffect = true,
            GUILightEffectsEnabled = true,
            BloomEnabled = true,
            Menu3DBackgroundEnabled = true,
        },

        // Very high preset
        new()
        {
            AntiAliasingMode = Settings.AntiAliasingMode.MSAA,
            MSAAResolution = Viewport.Msaa.Msaa8X,
            AnisotropicFilterLevel = Viewport.AnisotropicFiltering.Anisotropy16X,
            RenderScale = 1,
            UpscalingMethod = Settings.UpscalingMode.Fsr2,
            ChromaticEnabled = true,
            DisplayBackgroundParticles = true,
            MicrobeDistortionStrength = 0.6f,
            MicrobeBackgroundBlurLowQuality = false,
            MicrobeBackgroundBlurStrength = 2,
            MicrobeRippleEffect = true,
            GUILightEffectsEnabled = true,
            BloomEnabled = true,
            Menu3DBackgroundEnabled = true,
        },
    ];

    public enum Preset
    {
        Custom,
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh,
    }

    /// <summary>
    ///   Calculates which graphics preset is active.
    /// </summary>
    /// <param name="settings">Current settings values</param>
    /// <returns>The preset (or custom) that matches the currently selected options</returns>
    /// <remarks>
    ///   <para>
    ///     Note that if the set of options that are affected is changed, also
    ///     <see cref="OptionsMenu.ApplyGraphicsPresetOptionsToControls"/> must be updated.
    ///   </para>
    /// </remarks>
    public static Preset GetPreset(Settings settings)
    {
        for (Preset i = Preset.VeryHigh; i > Preset.Custom; --i)
        {
            // Check the highest preset first so that we match the highest quality the settings qualify for
            if (Presets[(int)i].Matches(settings))
                return i;
        }

        // If nothing matched, then is custom
        return Preset.Custom;
    }

    public static void ApplyPreset(Preset preset, Settings settings)
    {
        if (preset == Preset.Custom)
            return;

        switch (preset)
        {
            case Preset.VeryLow:
            case Preset.Low:
            case Preset.Medium:
            case Preset.High:
            case Preset.VeryHigh:
            {
                Presets[(int)preset].Apply(settings);
                break;
            }

            default:
                GD.PrintErr("Unknown preset to apply to settings: ", preset);
                break;
        }
    }

    private class PresetConfiguration
    {
        public Settings.AntiAliasingMode AntiAliasingMode { get; init; }
        public Viewport.Msaa MSAAResolution { get; init; }
        public Viewport.AnisotropicFiltering AnisotropicFilterLevel { get; init; }
        public float RenderScale { get; init; }
        public Settings.UpscalingMode UpscalingMethod { get; init; }
        public bool ChromaticEnabled { get; init; }
        public bool DisplayBackgroundParticles { get; init; }
        public float MicrobeDistortionStrength { get; init; }
        public bool MicrobeBackgroundBlurLowQuality { get; init; }
        public int MicrobeBackgroundBlurStrength { get; init; }
        public bool MicrobeRippleEffect { get; init; }
        public bool GUILightEffectsEnabled { get; init; }
        public bool BloomEnabled { get; init; }
        public bool Menu3DBackgroundEnabled { get; init; }

        public bool Matches(Settings settings)
        {
            if (settings.AntiAliasing.Value != AntiAliasingMode)
                return false;
            if (settings.AnisotropicFilterLevel.Value != AnisotropicFilterLevel)
                return false;
            if (settings.UpscalingMethod.Value != UpscalingMethod)
                return false;
            if (settings.ChromaticEnabled.Value != ChromaticEnabled)
                return false;
            if (settings.DisplayBackgroundParticles.Value != DisplayBackgroundParticles)
                return false;
            if (settings.MicrobeBackgroundBlurLowQuality.Value != MicrobeBackgroundBlurLowQuality)
                return false;
            if (settings.MicrobeRippleEffect.Value != MicrobeRippleEffect)
                return false;
            if (settings.GUILightEffectsEnabled.Value != GUILightEffectsEnabled)
                return false;
            if (settings.BloomEnabled.Value != BloomEnabled)
                return false;
            if (settings.Menu3DBackgroundEnabled.Value != Menu3DBackgroundEnabled)
                return false;

            // These settings allow a bit of wiggle room
            if (AntiAliasingMode is Settings.AntiAliasingMode.MSAA or Settings.AntiAliasingMode.MSAAAndTemporal)
            {
                // MSAA setting only matters if it is enabled
                if (settings.MSAAResolution.Value != MSAAResolution)
                    return false;
            }

            if (Math.Abs(settings.RenderScale.Value - RenderScale) > 0.01)
                return false;

            if (settings.MicrobeDistortionStrength.Value < MicrobeDistortionStrength - 0.01f)
                return false;

            // This value being above 0 enables it, so it is in the same performance class if both sides match
            if ((settings.MicrobeBackgroundBlurStrength.Value > 0) != (MicrobeBackgroundBlurStrength > 0))
                return false;

            return true;
        }

        public void Apply(Settings settings)
        {
            settings.AntiAliasing.Value = AntiAliasingMode;

            if (AntiAliasingMode is Settings.AntiAliasingMode.MSAA or Settings.AntiAliasingMode.MSAAAndTemporal)
                settings.MSAAResolution.Value = MSAAResolution;

            settings.AnisotropicFilterLevel.Value = AnisotropicFilterLevel;
            settings.RenderScale.Value = RenderScale;
            settings.UpscalingMethod.Value = UpscalingMethod;
            settings.ChromaticEnabled.Value = ChromaticEnabled;
            settings.DisplayBackgroundParticles.Value = DisplayBackgroundParticles;
            settings.MicrobeDistortionStrength.Value = MicrobeDistortionStrength;
            settings.MicrobeBackgroundBlurLowQuality.Value = MicrobeBackgroundBlurLowQuality;
            settings.MicrobeBackgroundBlurStrength.Value = MicrobeBackgroundBlurStrength;
            settings.MicrobeRippleEffect.Value = MicrobeRippleEffect;
            settings.GUILightEffectsEnabled.Value = GUILightEffectsEnabled;
            settings.BloomEnabled.Value = BloomEnabled;
            settings.Menu3DBackgroundEnabled.Value = Menu3DBackgroundEnabled;
        }
    }
}
