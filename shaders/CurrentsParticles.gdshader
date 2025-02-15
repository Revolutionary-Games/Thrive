shader_type particles;
render_mode disable_velocity;

uniform vec3 emission_box_extents;
uniform vec4 colorValue : source_color;
uniform sampler2D alphaCurve : repeat_disable;

uniform float gameTime;

uniform sampler3D noiseDisturbancesX;
uniform sampler3D noiseDisturbancesY;

uniform sampler3D noiseCurrentsX;
uniform sampler3D noiseCurrentsY;

float rand_from_seed(inout uint seed) {
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

float rand_from_seed_m1_p1(inout uint seed) {
    return rand_from_seed(seed) * 2.0 - 1.0;
}

uint hash(uint x) {
    x = ((x >> uint(16)) ^ x) * uint(73244475);
    x = ((x >> uint(16)) ^ x) * uint(73244475);
    x = (x >> uint(16)) ^ x;
    return x;
}

vec3 calculate_initial_position(inout uint alt_seed) {
    float pi = 3.14159;
    vec3 pos = vec3(0.0);
    { // Emission shape.
        pos = vec3(rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0) * emission_box_extents;
    }
    return pos;
}

void start() {
    uint base_number = NUMBER;
    uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);
    if (rand_from_seed(alt_seed) > AMOUNT_RATIO) {
        ACTIVE = false;
    }

    CUSTOM = vec4(0.0);
    COLOR = colorValue;
    TRANSFORM[0].xyz = vec3(1.0, 0.0, 0.0);
    TRANSFORM[1].xyz = vec3(0.0, 1.0, 0.0);
    TRANSFORM[2].xyz = vec3(0.0, 0.0, 1.0);
    TRANSFORM[3].xyz = calculate_initial_position(alt_seed);
    TRANSFORM = EMISSION_TRANSFORM * TRANSFORM;
}

void process() {
    CUSTOM.y += DELTA;
    float lifetime_percent = CUSTOM.y / LIFETIME;
    if (CUSTOM.y > LIFETIME) {
        ACTIVE = false;
    }
    
    vec2 current;
    {
        vec2 position = TRANSFORM[3].xz;
        
        // TODO: move constants to uniforms
        float timescale = 1.0f / 500.0f;
        float ratio = 0.15f;
        
        float disturbanceX = texture(noiseDisturbancesX, vec3(position, gameTime) / 64.0f).r;
        float disturbanceY = texture(noiseDisturbancesY, vec3(position, gameTime) / 64.0f).r;
        
        float currentX = texture(noiseCurrentsX, vec3(position.x * 0.1f, position.y, gameTime * timescale) / 64.0f).r; 
        float currentY = texture(noiseCurrentsY, vec3(position.x * 0.1f, position.y, gameTime * timescale) / 64.0f).r; 
        
        vec2 disturbances = vec2(disturbanceX, disturbanceY);
        vec2 currents = vec2(currentX, currentY);
        
        if (abs(currents.x) < 0.4)
        {
            currents.x = 0.0f;
        }
        
        if (abs(currents.y) < 0.4)
        {
            currents.y = 0.0f;
        }
        
        current = mix(currents, disturbances, ratio) * 2.0f - 1.0f;
        current *= 10.0f;
    }
    
    VELOCITY = vec3(current.x, 0.0f, -current.y);

    TRANSFORM[3].xyz += VELOCITY * DELTA;

    vec4 finalColor = colorValue;
    finalColor.a *= texture(alphaCurve, vec2(lifetime_percent)).r;
    
    COLOR = finalColor;

    TRANSFORM[0].xyz = normalize(TRANSFORM[0].xyz);
    TRANSFORM[1].xyz = normalize(TRANSFORM[1].xyz);
    TRANSFORM[2].xyz = normalize(TRANSFORM[2].xyz);

    CUSTOM.z = lifetime_percent;
}
