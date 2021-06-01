shader_type spatial;
render_mode blend_add;
render_mode unshaded;

uniform sampler2D densities;
uniform sampler2D noise;

uniform vec4 colour1 : hint_color = vec4(0, 0, 0, 0);
uniform vec4 colour2 : hint_color  = vec4(0, 0, 0, 0);
uniform vec4 colour3 : hint_color  = vec4(0, 0, 0, 0);
uniform vec4 colour4 : hint_color  = vec4(0, 0, 0, 0);

uniform vec2 UVOffset = vec2(0, 0);
uniform vec2 NoiseUVOffset = vec2(0, 0);

// Setting this too low makes the clouds invisible
const float CLOUD_DISSIPATION = 0.9f;

// Should be the same as its counterpart in CompoundCloudPlane.cs
const float CLOUD_MAX_INTENSITY_SHOWN = 1000.f;

const float NOISE_ZERO_OFFSET = 0.5f;

// Needs to match the value in CompoundCloudPlane.cs
const float NOISE_UV_SCALE = 2.5f;

float getIntensity(float value){
    return min(0.9 * atan(0.003f * CLOUD_MAX_INTENSITY_SHOWN * value), 1.0f);
    // return min(0.008 * CLOUD_MAX_INTENSITY_SHOWN * value, 1.0f);
    // return min(value * 0.9f, 1.0f);
}

void fragment(){
    vec4 concentrations = texture(densities, UV + UVOffset);
    
//    float cloud1 = max(getIntensity(concentrations.r) * (texture(noise, UV + UVOffset).r - NOISE_ZERO_OFFSET) * CLOUD_DISSIPATION, 0.f);
//
//    float cloud2 = max(getIntensity(concentrations.g) * (texture(noise, UV + UVOffset + 0.2f).r - NOISE_ZERO_OFFSET) * CLOUD_DISSIPATION, 0.f);
//
//    float cloud3 = max(getIntensity(concentrations.b) * (texture(noise, UV + UVOffset + 0.4f).r - NOISE_ZERO_OFFSET) * CLOUD_DISSIPATION, 0.f);
//
//    float cloud4 = max(getIntensity(concentrations.a) * (texture(noise, UV + UVOffset + 0.6f).r - NOISE_ZERO_OFFSET) * CLOUD_DISSIPATION, 0.f);
    
    float cloud1 = getIntensity(concentrations.r) * max(texture(noise, UV * NOISE_UV_SCALE + NoiseUVOffset).r - NOISE_ZERO_OFFSET, 0.0f) * CLOUD_DISSIPATION;
        
    float cloud2 = getIntensity(concentrations.g) * max(texture(noise, UV * NOISE_UV_SCALE + NoiseUVOffset + 0.2f).r - NOISE_ZERO_OFFSET, 0.0f) * CLOUD_DISSIPATION;
        
    float cloud3 = getIntensity(concentrations.b) * max(texture(noise, UV * NOISE_UV_SCALE + NoiseUVOffset + 0.4f).r - NOISE_ZERO_OFFSET, 0.0f) * CLOUD_DISSIPATION;
        
    float cloud4 = getIntensity(concentrations.a) * max(texture(noise, UV * NOISE_UV_SCALE + NoiseUVOffset + 0.6f).r - NOISE_ZERO_OFFSET, 0.0f) * CLOUD_DISSIPATION;
    
    vec4 colour =
        // first
        colour1 * cloud1
        + // second
        colour2 * cloud2
        + // third
        colour3 * cloud3
        + // fourth
        colour4 * cloud4;
    
    ALPHA = min(cloud1 + cloud2 + cloud3 + cloud4, 0.9f);
    
    ALBEDO = colour.rgb;
}
