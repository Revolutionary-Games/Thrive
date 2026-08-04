#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16f, set = 0, binding = 0) uniform image2D color_image;
layout(set = 0, binding = 1) uniform sampler2D depth_sampler;
layout(set = 0, binding = 2) uniform sampler3D base_noise;

layout(set = 0, binding = 3, std140) uniform Params {
    vec4 planet_center;       // .xyz
    vec4 shell;               // .x = inner radius (R + h0), .y = outer radius (R + h1)
    vec4 screen_size_padded;  // padded, .zw unused.
    vec4 cam_pos;             // .xyz world camera position
} p;

layout(push_constant, std430) uniform Matrices {
    mat4 inv_projection;      // reconstruct world ray
    mat4 cam_transform;
} m;

// === Ray/sphere: returns (near, far) t along ray, or miss ===
bool ray_sphere(vec3 ro, vec3 rd, vec3 center, float radius, out float t0, out float t1) {
    vec3 oc = ro - center;
    float b = dot(oc, rd);
    float c = dot(oc, oc) - radius * radius;
    float h = b * b - c;
    if (h < 0.0) return false;
    h = sqrt(h);
    t0 = -b - h;
    t1 = -b + h;
    return true;
}

const float base_scale = 0.001;
const float cloud_tile_size = 200.0;
const float detail_scale = 0.005;
const float density_multiplier = 1.0;
const vec3 wind = vec3(1.0, 0.0, 0.0);
const float time = 0.0;
const float scatter_strength = 1.0;

const int LIGHT_STEPS = 6;
const float LIGHT_STEP_SIZE = 0.05;

float height_gradient(float shell_frac) {
    return smoothstep(0.0, 0.1, shell_frac) * smoothstep(1.0, 0.6, shell_frac);
}

float remap(float v, float inMin, float inMax, float outMin, float outMax) {
    return outMin + (v - inMin) / (inMax - inMin) * (outMax - outMin);
}

// Henyey-Greenstein phase function
float henyey_greenstein(float cos_angle, float g) {
    float g2 = g * g;
    return (1.0 - g2) / (4.0 * 3.14159265 * pow(1.0 + g2 - 2.0 * g * cos_angle, 1.5));
}

// Beer-Powder: Beer attenuation + powder
float beer_powder(float density_along_light) {
    float beer = exp(-density_along_light);
    float powder = 1.0 - exp(-density_along_light * 2.0);
    return beer * powder * 2.0; // the 2.0 renormalizes the powder dip
}

vec3 sky_color(vec3 rd) {
    float up = clamp(rd.y * 0.5 + 0.5, 0.0, 1.0); // 0 horizon, 1 zenith
    vec3 zenith  = vec3(0.25, 0.45, 0.85);
    vec3 horizon = vec3(0.55, 0.72, 0.90);
    return mix(horizon, zenith, up);
}

vec3 aces(vec3 x) {
    return clamp((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14), 0.0, 1.0);
}

float sample_density(vec3 pos, float shell_frac, vec3 center) {
    vec3 rel = pos - center;
    vec3 sample_pos = rel / cloud_tile_size;
    vec4 base = texture(base_noise, sample_pos);
    float shape = base.r;

    float hg_grad = height_gradient(shell_frac);
    float coverage = 0.3;
    float density = clamp(remap(shape, 1.0 - coverage, 1.0, 0.0, 1.0), 0.0, 1.0);
    density *= hg_grad;

    // detail erosion
    vec3 detail_pos = rel / (cloud_tile_size / 8.0);
    vec4 detail = texture(base_noise, detail_pos);
    float detail_fbm = detail.g * 0.625 + detail.b * 0.25 + detail.a * 0.125;
    float erosion = detail_fbm * (1.0 - shell_frac);
    density = clamp(remap(density, erosion * 0.2, 1.0, 0.0, 1.0), 0.0, 1.0);

    return density * density_multiplier;
}

float light_march(vec3 pos, vec3 sun_dir, float inner, float outer, vec3 center) {
    float shell_thickness = outer - inner;
    float step_len = shell_thickness * LIGHT_STEP_SIZE;
    float optical_depth = 0.0;
    vec3 lp = pos;
    for (int j = 0; j < LIGHT_STEPS; j++) {
        lp += sun_dir * step_len;
        float h = length(lp - center);
        float sf = clamp((h - inner) / (outer - inner), 0.0, 1.0);
        float d = sample_density(lp, sf, center);
        optical_depth += d * step_len;
    }
    return optical_depth;
}

void main() {
    ivec2 px = ivec2(gl_GlobalInvocationID.xy);
    if (px.x >= int(p.screen_size_padded.x) || px.y >= int(p.screen_size_padded.y))
        return;

    vec2 screen_size = p.screen_size_padded.xy;
    vec2 uv = (vec2(px) + 0.5) / screen_size;
    vec2 ndc = uv * 2.0 - 1.0;

    vec4 target = m.inv_projection * vec4(ndc.x, ndc.y, 1.0, 1.0);
    vec3 view_rd = normalize(target.xyz / target.w);
    
    vec3 rd = normalize((m.cam_transform * vec4(view_rd, 0.0)).xyz);
    
    vec3 ro = p.cam_pos.xyz;
    vec3 center = p.planet_center.xyz;

    float outer = p.shell.y;
    float inner = p.shell.x;

    float t_max = 1e9;
    float raw_depth = texelFetch(depth_sampler, px, 0).r;
    
    if (raw_depth > 0.0) {
        vec4 view_pos = m.inv_projection * vec4(ndc, raw_depth, 1.0);
        view_pos.xyz /= view_pos.w;
        
        vec3 world_pos = (m.cam_transform * vec4(view_pos.xyz, 1.0)).xyz;
        t_max = length(world_pos - ro);
    }

    // Determine the [march_start, march_end] interval inside the shell.
    // Three cases:
    //   (a) below shell (on ground): march from outer-entry... down to inner, complex
    //   (b) inside shell (fly-through): start at camera (t=0 / near plane)
    //   (c) above shell (space): march the outer intersection down to inner
    float march_start, march_end;

    float ot0, ot1, it0, it1;

    bool hit_outer = ray_sphere(ro, rd, center, outer, ot0, ot1);
    bool hit_inner = ray_sphere(ro, rd, center, inner, it0, it1);

    if (!hit_outer) {
        return; // never touches the atmosphere at all
    }

    float cam_r = length(ro - center);

    if (cam_r > outer) {
        // ABOVE the shell (in space): enter outer, exit at inner if hit, else outer far side
        march_start = ot0;
        march_end   = hit_inner ? it0 : ot1;
    }
    else if (cam_r < inner) {
        // BELOW the shell (under the deck, your case at r=2001, inner=2200):
        // march only if the ray goes UP into the deck. It enters the inner sphere at it1
        // (far root, because we're inside the inner sphere) and exits outer at ot1.
        if (!hit_inner) { return; }      // shouldn't happen when inside inner, but guard
        march_start = it1;               // where ray punches through the deck's underside
        march_end   = ot1;               // where it leaves the top of the deck
    }
    else {
        // INSIDE the shell itself (in the clouds): from camera to whichever boundary is hit first
        march_start = 0.0;
        march_end   = hit_inner ? it0 : ot1;
        // (if the ray dips toward the planet it exits via inner at it0; else via outer at ot1)
    }

    march_start = max(march_start, 0.0);
    march_end   = min(march_end, t_max);

    if (march_end <= march_start) {
        return; // no valid interval — leave scene untouched, DON'T march a negative dt
    }

    // March: density + beer
    const int STEPS = 96;
    float dt = (march_end - march_start) / float(STEPS);
    float jitter = fract(sin(dot(vec2(px), vec2(12.9898, 78.233))) * 43758.5453);
    float t = march_start + dt * jitter;
    vec3 sun_dir = normalize(vec3(1.0, 1.0, 1.0));
    float cos_angle = dot(rd, sun_dir);
    float phase = henyey_greenstein(cos_angle, 0.1); // g=0.3 forward scatter; tune 0.1-0.5

    vec3 sun_color = vec3(1.0, 0.92, 0.78);   // warm sunlight
    vec3 ambient   = vec3(0.45, 0.55, 0.7) * 0.5; 
    vec3 accumulated_light = vec3(0.0);
    float transmittance = 1.0;

    for (int i = 0; i < STEPS; i++) {
        vec3 pos = ro + rd * t;
        float h = length(pos - center);
        float shell_frac = clamp((h - inner) / (outer - inner), 0.0, 1.0);

        float density = sample_density(pos, shell_frac, center);

        if (density > 0.001) {
            // how much sunlight reaches this point
            float light_optical_depth = light_march(pos, sun_dir, inner, outer, center);
            float light_transmittance = beer_powder(light_optical_depth);

            // light scattered toward the eye from this sample
            vec3 sample_light = sun_color * light_transmittance * phase  + ambient * 0.3;
            accumulated_light += transmittance * sample_light * density * dt * scatter_strength;

            transmittance *= exp(-density * dt);

            if (transmittance < 0.01)
                break;
        }
        t += dt;
    }

    float cloud_alpha = 1.0 - transmittance;
    vec3 scene = imageLoad(color_image, px).rgb;

    vec3 sky = sky_color(rd);
    sky += sun_color * pow(max(dot(rd, sun_dir), 0.0), 8.0) * 0.5; // sun glow
    vec3 bg = (raw_depth > 0.0) ? scene : sky;

    vec3 outc = bg * transmittance + accumulated_light;
    outc = outc / (outc + vec3(1.0)); // Reinhardt
    float lum = dot(outc, vec3(0.2126, 0.7152, 0.0722));
    outc = mix(vec3(lum), outc, 1.3);
    imageStore(color_image, px, vec4(outc, 1.0));
}