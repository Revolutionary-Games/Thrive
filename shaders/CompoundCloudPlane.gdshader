shader_type spatial;
render_mode blend_mix;
render_mode unshaded;

uniform sampler2D densities;
uniform sampler2D noise;

uniform vec4 colour1 : source_color = vec4(1.0, 1.0, 0.0, 1.0);
uniform vec4 colour2 : source_color = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec4 colour3 : source_color = vec4(0.0, 1.0, 0.0, 1.0);
uniform vec4 colour4 : source_color = vec4(1.0, 0.0, 0.0, 1.0);

uniform vec2 UVOffset = vec2(0, 0);

// This must evenly divide into 1 or tiling will break
uniform float NoiseScale = 0.05;

uniform float BrightnessMultiplier = 2.0;
uniform float CLOUD_SPEED = 0.013;

// Setting this too low makes the clouds invisible
const float CLOUD_DISSIPATION = 0.7;
const float DENSITY_MULTIPLIER = 0.95;

// Should be the same as its counterpart in CompoundCloudPlane.cs
const float CLOUD_MAX_INTENSITY_SHOWN = 1000.f;

float getIntensity(float value) 
{
    return min(DENSITY_MULTIPLIER * atan(0.006f * CLOUD_MAX_INTENSITY_SHOWN * value), 1.0f) * BrightnessMultiplier;
}

void fragment() {
    vec2 scaledUV = UV + UVOffset;
	vec2 noiseUV = scaledUV / NoiseScale;

    vec4 concentrations = texture(densities, scaledUV);

    // Calculates the intensities for each color channel containing the concentrations
    float cloud1 = getIntensity(concentrations.r) *
        texture(noise, noiseUV + vec2(TIME * CLOUD_SPEED, 0)).r *
        texture(noise, noiseUV + vec2(0, TIME * CLOUD_SPEED)).r * CLOUD_DISSIPATION;

    float cloud2 = getIntensity(concentrations.g) *
        texture(noise, noiseUV + vec2(TIME * CLOUD_SPEED, 0) + 0.2).r *
        texture(noise, noiseUV + vec2(0, TIME * CLOUD_SPEED) + 0.2).r * CLOUD_DISSIPATION;

    float cloud3 = getIntensity(concentrations.b) *
        texture(noise, noiseUV + vec2(TIME * CLOUD_SPEED, 0) + 0.4).r *
	    texture(noise, noiseUV + vec2(0, TIME * CLOUD_SPEED) + 0.4).r * CLOUD_DISSIPATION;

    float cloud4 = getIntensity(concentrations.a) *
        texture(noise, noiseUV + vec2(TIME * CLOUD_SPEED, 0) + 0.6).r *
        texture(noise, noiseUV + vec2(0, TIME * CLOUD_SPEED) + 0.6).r * CLOUD_DISSIPATION;

    // Sum everything and we also ensure that even the clouds with the lowest amount of compounds get some colour too
    vec4 colour = colour1 * cloud1 + 
                  colour2 * cloud2 + 
                  colour3 * cloud3 + 
                  colour4 * cloud4;

    float adjustedAlpha = min(cloud1 + cloud2 + cloud3 + cloud4, 1.0) * clamp(min(cloud1 + cloud2 + cloud3 + cloud4, 1.0) * 0.3, 0.3, 0.7);

    ALPHA = adjustedAlpha;
    ALBEDO = colour.rgb;
}
