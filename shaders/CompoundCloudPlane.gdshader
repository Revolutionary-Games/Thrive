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

uniform float BrightnessMultiplier = 0.10;
uniform float NoiseScale = 0.1;

// Setting this too low makes the clouds invisible
const float CLOUD_DISSIPATION = 0.70;
const float DENSITY_MULTIPLIER = 0.95;

// Should be the same as its counterpart in CompoundCloudPlane.cs
const float CLOUD_MAX_INTENSITY_SHOWN = 1000.0;

// This needs to be less than 0.5 otherwise large cloud areas (due to the noise texture
// not being very high resolution) are invisible
const float NOISE_ZERO_OFFSET = 0.001;

//You could be asking why there are two intensities values? One handles the cloud itself while the other has to take care of the noise itself
//Probably could be further optimized

const float NOISE_INTENSITY = 0.09;

float getIntensity(float value) 
{
    return min(DENSITY_MULTIPLIER * atan(0.006 * CLOUD_MAX_INTENSITY_SHOWN * value), 1.0) * BrightnessMultiplier;
}

//Makes the noise sparse, i did my best to optimize the function

float sparseNoise(vec2 uv, float scale, float contrast) 
{
    float noiseValue = texture(noise, uv * scale).r;
    noiseValue = 0.5 + contrast * (noiseValue - 0.5);
    
    vec2 offsets[8] = vec2[]
    (
        vec2(1.0, 0.0),
        vec2(-1.0, 0.0),
        vec2(0.0, 1.0),
        vec2(0.0, -1.0),
        vec2(1.0, 1.0),
        vec2(-1.0, -1.0),
        vec2(1.0, -1.0),
        vec2(-1.0, 1.0)
    );
    
    float sum = noiseValue;
    for (int i = 0; i < 8; i++) 
    {
        sum += texture(noise, uv + offsets[i] * scale).r;
    }
    
    float smoothedNoise = sum / 9.0;
    return max(smoothedNoise, 0.05); 
}

void fragment() {
    vec2 scaledUV = UV + UVOffset;

    vec4 concentrations = texture(densities, scaledUV) / NoiseScale;

    float noiseValue = sparseNoise(scaledUV / NoiseScale, 0.5, 1.0);
    float noiseFactor = max(noiseValue - NOISE_ZERO_OFFSET, 0.0);

    // Calculates the intensities for each color channel containing the concentrations
    float cloud1 = getIntensity(concentrations.r) * noiseFactor * CLOUD_DISSIPATION;
    float cloud2 = getIntensity(concentrations.g) * noiseFactor * CLOUD_DISSIPATION;
    float cloud3 = getIntensity(concentrations.b) * noiseFactor * CLOUD_DISSIPATION;
    float cloud4 = getIntensity(concentrations.a) * noiseFactor * CLOUD_DISSIPATION;

    // Sum everything and we also ensure that even the clouds with the lowest amount of compounds get some colour too
    vec4 colour = colour1 * max(cloud1, NOISE_INTENSITY * noiseFactor) + 
                  colour2 * max(cloud2, NOISE_INTENSITY * noiseFactor) + 
                  colour3 * max(cloud3, NOISE_INTENSITY * noiseFactor) + 
                  colour4 * max(cloud4, NOISE_INTENSITY * noiseFactor);
    

    float edgeNoiseValue = texture(noise, scaledUV * 0.5).r;
    float edgeFactor = smoothstep(0.0, 1.0, edgeNoiseValue);

    vec4 edgeColor = max(edgeFactor * 1.2 * (colour1 * concentrations.r + colour2 * concentrations.g + colour3 * concentrations.b + colour4 * concentrations.a), vec4(0.0));
    vec4 finalColor = mix(colour, edgeColor, 0.9);

    float adjustedAlpha = min(cloud1 + cloud2 + cloud3 + cloud4, 1.0) * clamp(min(cloud1 + cloud2 + cloud3 + cloud4, 1.0) * 0.3, 0.3, 0.7);
    ALPHA = adjustedAlpha;
    ALBEDO = finalColor.rgb * min(1.0, 0.75 * edgeColor.a);
}
