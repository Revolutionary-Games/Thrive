shader_type spatial;
render_mode blend_mix;
render_mode unshaded;

uniform sampler2D densities;
uniform sampler2D noise;

uniform vec4 colour1 : hint_color = vec4(0, 0, 0, 0);
uniform vec4 colour2 : hint_color = vec4(0, 0, 0, 0);
uniform vec4 colour3 : hint_color = vec4(0, 0, 0, 0);
uniform vec4 colour4 : hint_color = vec4(0, 0, 0, 0);

uniform vec2 UVOffset = vec2(0, 0);

uniform float BrightnessMultiplier = 1.0f;
uniform float CloudAlpha = 0.7f;

// Setting this too low makes the clouds invisible
const float CLOUD_DISSIPATION = 0.70f;

const float DENSITY_MULTIPLIER = 0.95f;

// Should be the same as its counterpart in CompoundCloudPlane.cs
const float CLOUD_MAX_INTENSITY_SHOWN = 1000.f;

// This needs to be less than 0.5 otherwise large cloud areas (due to the noise texture 
// not being very high resolution) are invisible
const float NOISE_ZERO_OFFSET = 0.001f;

float getIntensity(float value){
    return min(DENSITY_MULTIPLIER * atan(0.006f * CLOUD_MAX_INTENSITY_SHOWN * value), 1.0f) * BrightnessMultiplier;
}

void fragment(){
    vec4 concentrations = texture(densities, UV + UVOffset);
    
    float cloud1 = getIntensity(concentrations.r) * max(texture(noise, UV + UVOffset).r - 
        NOISE_ZERO_OFFSET, 0.0f) * CLOUD_DISSIPATION;
        
    float cloud2 = getIntensity(concentrations.g) * max(texture(noise, UV + UVOffset + 0.2f).r - 
        NOISE_ZERO_OFFSET, 0.0f) * CLOUD_DISSIPATION;
        
    float cloud3 = getIntensity(concentrations.b) * max(texture(noise, UV + UVOffset + 0.4f).r - 
        NOISE_ZERO_OFFSET, 0.0f) * CLOUD_DISSIPATION;
        
    float cloud4 = getIntensity(concentrations.a) * max(texture(noise, UV + UVOffset + 0.6f).r - 
        NOISE_ZERO_OFFSET, 0.0f) * CLOUD_DISSIPATION;
    
    vec4 colour =
        // first
        colour1 * cloud1
        + // second
        colour2 * cloud2
        + // third
        colour3 * cloud3
        + // fourth
        colour4 * cloud4;
    
    ALPHA = min(cloud1 + cloud2 + cloud3 + cloud4, 1.0f) * CloudAlpha;
    
    ALBEDO = colour.rgb;
}
