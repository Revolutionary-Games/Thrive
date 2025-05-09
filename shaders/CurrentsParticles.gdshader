shader_type particles;
render_mode disable_velocity;

uniform vec3 emissionBoxExtents;
uniform vec4 colorValue : source_color;
uniform sampler2D alphaCurve : repeat_disable;

uniform float gameTime;

uniform sampler3D noiseDisturbancesX;
uniform sampler3D noiseDisturbancesY;

uniform sampler3D noiseCurrentsX;
uniform sampler3D noiseCurrentsY;

uniform float speed;
uniform float chaoticness;
uniform float scale;

uniform float brightness;

// The following constants should be the same as in FluidCurrentsSystem.cs
const float disturbanceTimescale = 1.0f;
const float currentsTimescale = 1.0f / 500.0f;
const float currentsStretchingMultiplier = 1.0f / 10.0f;
const float disturbanceToCurrentsRatio = 0.15f;
const float minCurrentIntensity = 0.25f;
const float positionScaling = 0.9f;

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
    float visibleScale = RandFromSeed(altSeed) * 0.75f + 0.625f;
    TRANSFORM[0].xyz *= visibleScale;
    TRANSFORM[1].xyz *= visibleScale;
    TRANSFORM[2].xyz *= visibleScale;
}

void process() {
    CUSTOM.y += DELTA;
    float lifetimePercent = CUSTOM.y / LIFETIME;
    if (CUSTOM.y > LIFETIME) {
        ACTIVE = false;
    }
    
    vec2 current;
    {
        // The following should strictly conform to the formula in FluidCurrentSystem.cs
        vec2 position = TRANSFORM[3].xz * positionScaling * scale;
        
        float disturbanceX = texture(noiseDisturbancesX,
            vec3(position / 128.0f, gameTime * chaoticness * disturbanceTimescale / 64.0f)).r;
        float disturbanceY = texture(noiseDisturbancesY,
            vec3(position / 128.0f, gameTime * chaoticness * disturbanceTimescale / 64.0f)).r;
        
        float currentX = texture(noiseCurrentsX,
            vec3(position.x * currentsStretchingMultiplier / 128.0f, position.y / 128.0f,
                gameTime * currentsTimescale * chaoticness / 64.0f)).r; 
        float currentY = texture(noiseCurrentsY,
            vec3(position.x / 128.0f, position.y * currentsStretchingMultiplier / 128.0f,
                gameTime * currentsTimescale * chaoticness / 64.0f)).r;
        
        vec2 disturbances = vec2(disturbanceX, disturbanceY) * 2.0f - 1.0f;
        vec2 currents = vec2(currentX, currentY) * 2.0f - 1.0f;
        
        if (pow(currents.x, 2) + pow(currents.y, 2) < minCurrentIntensity)
        {
            currents = vec2(0.0f);
        }
        
        current = mix(currents, disturbances, disturbanceToCurrentsRatio);
        current *= 10.0f * speed;
    }
    
    VELOCITY = mix(vec3(current.x, 0.0f, current.y), VELOCITY, 1.0f - DELTA * 5.0f);

    TRANSFORM[3].xyz += VELOCITY * DELTA;
    
    vec4 finalColor = colorValue;
    finalColor.a *= texture(alphaCurve, vec2(lifetimePercent)).r;
    if (speed != 0.0)
    {
        // The faster the particle, the more visible it is
        finalColor.a *= 0.2f + 0.8f * length(VELOCITY) / (speed * 10.0f);
    }
    
    COLOR = finalColor * brightness;
}
