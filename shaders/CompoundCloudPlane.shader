shader_type spatial;
render_mode blend_add;
render_mode unshaded;
render_mode world_vertex_coords;

uniform sampler2D icon1;
uniform sampler2D icon2;
uniform sampler2D icon3;
uniform sampler2D icon4;

uniform float scale = 0.01;
uniform float texture_multiplier = 0.5;
uniform float blend_factor = 0.5;
uniform bool enable_texture = false;
varying vec3 global_uv; // the global uv is global position

uniform float coumpound_count = 4.0;
uniform int compound_count_offset = 0;

uniform sampler2D densities;
uniform sampler2D noise;

uniform vec4 colour1 : hint_color = vec4(0, 0, 0, 0);
uniform vec4 colour2 : hint_color = vec4(0, 0, 0, 0);
uniform vec4 colour3 : hint_color = vec4(0, 0, 0, 0);
uniform vec4 colour4 : hint_color = vec4(0, 0, 0, 0);

uniform vec2 UVOffset = vec2(0, 0);

uniform float BrightnessMultiplier = 1.0f;

// Setting this too low makes the clouds invisible
const float CLOUD_DISSIPATION = 0.9f;

const float DENSITY_MULTIPLIER = 0.95f;

// Should be the same as its counterpart in CompoundCloudPlane.cs
const float CLOUD_MAX_INTENSITY_SHOWN = 1000.f;

// This needs to be less than 0.5 otherwise large cloud areas (due to the noise texture 
// not being very high resolution) are invisible
const float NOISE_ZERO_OFFSET = 0.45f;

// Needs to match the value in CompoundCloudPlane.cs
// TODO: implement this or increase the perlin noise texture size to give the clouds more detail
const float NOISE_UV_SCALE = 2.5f;

void vertex() {
    //creates a global uv, but works only on xz axis. other axis have not been taken into account
    global_uv = VERTEX;
    global_uv *= vec3(1.0, -1.0, 1.0);
}

//checks wether given uv point is black/white on checker pattern
float checker_pattern(vec2 uv2) {
    uv2 = floor(uv2);
    float filter = 1.0 - mod(uv2.y + uv2.x / 2., coumpound_count / 2.);
    return floor(clamp(filter, 0., 1.));
}

//returns icon i at coordinate uv
vec4 icon(int i, vec2 uv) {
    switch (i) {
        case 0:
            return texture(icon1, uv);
        case 1:
            return texture(icon2, uv);
        case 2:
            return texture(icon3, uv);
        case 3:
            return texture(icon4, uv);
    }
}


vec4 get_pattern(int icon_index) {
    float pattern_size = 1.0 / scale;
    vec2 tile_size = 1.0/vec2(pattern_size);

    vec4 icon_texture = vec4(0.);
    vec2 offset = tile_size * vec2(float(icon_index + compound_count_offset),0.);
    
    vec2 uv = global_uv.xz + offset + UVOffset;
    uv *= pattern_size;

    float checker = checker_pattern(uv);
    // if (checker == 1.0)
    if (checker > 0.9) icon_texture = icon(icon_index, uv);

    return icon_texture;
}

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
    
    vec4 swap_colour1 = colour1;
    vec4 swap_colour2 = colour2;
    vec4 swap_colour3 = colour3;
    vec4 swap_colour4 = colour4;

    if (enable_texture) {
        vec4 tex_colour1 = get_pattern(0) * texture_multiplier;
        vec4 tex_colour2 = get_pattern(1) * texture_multiplier;
        vec4 tex_colour3 = get_pattern(2) * texture_multiplier;
        vec4 tex_colour4 = get_pattern(3) * texture_multiplier;
        
        swap_colour1 = mix(tex_colour1, swap_colour1, blend_factor);
        swap_colour2 = mix(tex_colour2, swap_colour2, blend_factor);
        swap_colour3 = mix(tex_colour3, swap_colour3, blend_factor);
        swap_colour4 = mix(tex_colour4, swap_colour4, blend_factor);
    }

    vec4 colour =
        // first
        swap_colour1 * cloud1 + // second
        swap_colour2 * cloud2 + // third
        swap_colour3 * cloud3 + // fourth
        swap_colour4 * cloud4;
    
    ALPHA = min(cloud1 + cloud2 + cloud3 + cloud4, 0.9f);
    
    ALBEDO = colour.rgb;
}
