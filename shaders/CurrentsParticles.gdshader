shader_type particles;

uniform vec3 emissionBoxExtents;
uniform vec4 colorValue : source_color;
uniform sampler2D alphaCurve : repeat_disable;

uniform float gameTime;

uniform sampler2D noiseCurrents1;
uniform sampler2D noiseCurrents2;

uniform float speed;
uniform float chaoticness;
uniform float inverseScale;

uniform float brightness;

// The following constants should be the same as in FluidCurrentsSystem.cs
const float currentsTimescale = 0.25f;
const float positionScaling = 0.9f;

uniform float particleDepthVariation = 1.0f;

// Generates a random float and modifies the seed
float RandFromSeed(inout uint seed) {
    int k;
    int s = int(seed);
    if (s == 0) {
        s = 305420679;
    }
    k = s / 127773;
    s = 16807 * (s - k * 127773) - 2836 * k;
    if (s < 0) {
        s += 2147483647;
    }
    seed = uint(s);
    return float(seed % uint(65536)) / 65535.0;
}

uint Hash(uint x) {
    x = ((x >> uint(16)) ^ x) * uint(73244475);
    x = ((x >> uint(16)) ^ x) * uint(73244475);
    x = (x >> uint(16)) ^ x;
    return x;
}

void start() {
    uint baseNumber = NUMBER;
    uint altSeed = Hash(baseNumber + uint(1) + RANDOM_SEED);

    CUSTOM = vec4(0.0);
    COLOR = colorValue;
    TRANSFORM[0].xyz = vec3(1.0, 0.0, 0.0);
    TRANSFORM[1].xyz = vec3(0.0, 1.0, 0.0);
    TRANSFORM[2].xyz = vec3(0.0, 0.0, 1.0);
    TRANSFORM[3].xyz = vec3(RandFromSeed(altSeed) * 2.0 - 1.0, RandFromSeed(altSeed) * 2.0 - 1.0,
        RandFromSeed(altSeed) * 2.0 - 1.0) * emissionBoxExtents;
    TRANSFORM = EMISSION_TRANSFORM * TRANSFORM;
    
    float backgroundFactor = RandFromSeed(altSeed) * particleDepthVariation;
    float adjustedBackgroundFactor = pow(backgroundFactor, 3.0f);
    TRANSFORM[3].y -= adjustedBackgroundFactor * 80.0f;
    float visibleScale = RandFromSeed(altSeed) * 0.75f + 0.625f
        + adjustedBackgroundFactor * 35.0f;
    
    TRANSFORM[0].xyz *= visibleScale;
    TRANSFORM[1].xyz *= visibleScale;
    TRANSFORM[2].xyz *= visibleScale;
    
    CUSTOM.w = 1.0f - backgroundFactor * 0.9f;
    CUSTOM.z = gameTime + LIFETIME * 0.5f;
}

void process() {
    CUSTOM.y += DELTA;
    float lifetimePercent = CUSTOM.y / (LIFETIME - 7.0f);
    if (CUSTOM.y > LIFETIME) {
        ACTIVE = false;
    }
    
    vec2 current;
    {
        // The following should strictly conform to the formula in FluidCurrentSystem.cs
        vec2 scaledPosition = TRANSFORM[3].xz * positionScaling * inverseScale;
        // CUSTOM.z is the point of time when the lead particle spawned, CUSTOM.y is this particle's lifetime.
        // The sum is the time such that this particle follows the exact same path as the lead one.
        // Or just the gameTime if this is the lead particle.
        float scaledTime = (CUSTOM.z + CUSTOM.y) * currentsTimescale * chaoticness;
        
        vec2 current1 = texture(noiseCurrents1,
            vec2((scaledPosition.x + scaledTime) / 1024.0f,
                (scaledPosition.y + scaledTime) / 1024.0f)).rg;
        vec2 current2 = texture(noiseCurrents2,
            vec2((scaledPosition.x - scaledTime) / 1024.0f,
                (scaledPosition.y - scaledTime) / 1024.0f)).rg;
        
        current = current1 * 2.0f - 1.0f;
        current *= current2;
        
        current *= 10.0f * speed;
    }
    
    VELOCITY = mix(vec3(current.x, 0.0f, current.y), VELOCITY, 1.0f - DELTA * 5.0f);
    VELOCITY *= CUSTOM.w * 0.2f + 0.8f;
    
    vec4 finalColor = colorValue;
    finalColor.rgba *= CUSTOM.w;
    finalColor.a *= texture(alphaCurve, vec2(lifetimePercent)).r;
    if (speed != 0.0)
    {
        // The faster the particle, the more visible it is
        finalColor.a *= 0.2f + 0.8f * length(VELOCITY) / (speed * 10.0f);
    }
    
    COLOR = finalColor * brightness;
}
