#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16f, set = 0, binding = 0) uniform image2D cloud_target;
layout(set = 0, binding = 1) uniform sampler2D depth_sampler;   // FULL resolution
layout(set = 0, binding = 2) uniform sampler3D base_noise;

layout(set = 0, binding = 3, std140) uniform Params {
    vec4 planet_center;   // .xyz center                          .w unused
    vec4 shell;           // .x inner R, .y outer R, .z tile size,.w density multiplier
    vec4 screen_size;     // .xy size,   .zw 1/size
    vec4 march_size;      // .xy MARCH res, .zw 1/MARCH res
    vec4 cam_pos;         // .xyz world camera position           .w unused
    vec4 sun;             // .xyz direction TOWARD sun (unit)     .w sun energy
    vec4 quality;         // .x base steps, .y light steps, .z max march dist, .w coverage
} p;

layout(push_constant, std430) uniform Matrices {
    mat4 inv_projection;
    mat4 cam_transform;
} m;

// ---------------------------------------------------------------------------
// Tunables. Perhaps these should be in an UBO later
// ---------------------------------------------------------------------------
const int   MS_OCTAVES        = 3;       // multiple-scattering approximation octaves
const float MS_ATTENUATION    = 0.55;    // energy falloff per octave
const float PHASE_G           = 0.8;     // forward lobe eccentricity
const float PHASE_BACK_G      = 0.25;    // backward lobe as a fraction of PHASE_G
const float PHASE_BACK_MIX    = 0.15;    // weight of the backward lobe
const float DETAIL_FREQ       = 8.0;     // detail tile = base tile / DETAIL_FREQ
const float COARSE_STEP_SCALE = 3.0;     // empty-space skip multiplier
const int   EMPTY_RUN_LIMIT   = 8;       // fine steps of nothing before going coarse again
const float LIGHT_STEP_FRAC   = 0.06;    // first light step as a fraction of shell thickness
const float LIGHT_STEP_GROW   = 1.45;    // light steps grow exponentially (cone-ish)
const vec3  SUN_TINT          = vec3(1.0, 0.95, 0.87);
const vec3  AMBIENT_TINT      = vec3(0.42, 0.55, 0.78);
const float AMBIENT_ENERGY    = 1.2;
const float AMBIENT_BASE_MUL  = 0.25;    // ambient at cloud base vs. cloud top
const float DISTANCE_LOD      = 0.00015; // step growth per unit of distance

const float PI = 3.14159265359;

// Globals
// Using globals here for the ray marching step to keep the code clean.
vec3  g_ro;
vec3  g_rd;
vec3  g_center;
vec3  g_sun_dir;
vec3  g_sun_energy;
vec3  g_ambient_energy;
float g_inner;
float g_outer;
float g_thickness;
float g_cos_angle;
float g_jitter;

vec3  g_accumulated;
float g_transmittance;

// ---------------------------------------------------------------------------
// Utility
// ---------------------------------------------------------------------------

// Numerically stable ray/sphere. The (|oc|-r)(|oc|+r) form avoids the catastrophic cancellation of dot(oc,oc) - r*r at
// planetary radii.
bool ray_sphere(vec3 ro, vec3 rd, vec3 center, float radius, out float t0, out float t1) {
    vec3 oc = ro - center;
    float l = length(oc);
    float b = dot(oc, rd);
    float c = (l - radius) * (l + radius);
    float h = b * b - c;
    if (h < 0.0) { t0 = 0.0; t1 = 0.0; return false; }
    h = sqrt(h);
    t0 = -b - h;
    t1 = -b + h;
    return true;
}

float remap(float v, float in_min, float in_max, float out_min, float out_max) {
    return out_min + (v - in_min) / (in_max - in_min) * (out_max - out_min);
}

// Interleaved gradient noise
float ign(vec2 q) {
    return fract(52.9829189 * fract(dot(q, vec2(0.06711056, 0.00583715))));
}

float henyey_greenstein(float cos_angle, float g) {
    float g2 = g * g;
    return (1.0 - g2) / (4.0 * PI * pow(max(1.0 + g2 - 2.0 * g * cos_angle, 1e-4), 1.5));
}

// Dual-lobe: forward scatter for silverlining.
float dual_lobe(float cos_angle, float g) {
    return mix(henyey_greenstein(cos_angle, g),
               henyey_greenstein(cos_angle, -g * 0.25),
               PHASE_BACK_MIX);
}

// Wrenninge-Frostbite octave approximation of multiple scattering.
float multi_scatter(float optical_depth, float cos_angle) {
    float a = 1.0;
    float b = 1.0;
    float c = 1.0;
    float lum = 0.0;
    for (int n = 0; n < MS_OCTAVES; n++) {
        lum += a * exp(-optical_depth * b) * dual_lobe(cos_angle, PHASE_G * c);
        a *= MS_ATTENUATION;
        b *= 0.5;
        c *= 0.5;
    }
    return lum;
}

float height_gradient(float shell_frac) {
    return smoothstep(0.0, 0.1, shell_frac) * smoothstep(1.0, 0.6, shell_frac);
}

// ---------------------------------------------------------------------------
// Density field
// ---------------------------------------------------------------------------
float sample_density(vec3 rel, float shell_frac, bool cheap) {
    float tile = p.shell.z;

    float shape = texture(base_noise, rel / tile).r;

    float coverage = p.quality.w;
    float density = clamp(remap(shape, 1.0 - coverage, 1.0, 0.0, 1.0), 0.0, 1.0);
    density *= height_gradient(shell_frac);

    if (density <= 0.0)
        return 0.0;

    if (!cheap) {
        vec4 detail = texture(base_noise, rel * (DETAIL_FREQ / tile));
        float detail_fbm = detail.g * 0.6098 + detail.b * 0.2439 + detail.a * 0.1463;
        float erosion = detail_fbm * (1.0 - shell_frac);
        density = clamp(remap(density, erosion * 0.2, 1.0, 0.0, 1.0), 0.0, 1.0);
    }

    return density * p.shell.w;
}

// Shadow ray toward the sun.
float light_march(vec3 rel) {
    int steps = int(p.quality.y);
    float step_len = g_thickness * LIGHT_STEP_FRAC;
    float optical_depth = 0.0;
    vec3 lp = rel;

    for (int j = 0; j < steps; j++) {
        lp += g_sun_dir * step_len;
        float h = length(lp);
        if (h < g_inner || h > g_outer)
            break;
        float sf = (h - g_inner) / g_thickness;
        optical_depth += sample_density(lp, sf, true) * step_len;
        step_len *= LIGHT_STEP_GROW;
    }

    return optical_depth;
}

// Ray march
void march_segment(float seg_start, float seg_end, float dt_base, int max_iter) {
    if (seg_end <= seg_start || g_transmittance < 0.01)
        return;

    float t = seg_start + dt_base * g_jitter;
    bool coarse = true;
    int empty_run = 0;

    for (int i = 0; i < max_iter; i++) {
        if (t >= seg_end)
            break;

        float dt_fine = dt_base * (1.0 + t * DISTANCE_LOD);
        float dt_coarse = dt_fine * COARSE_STEP_SCALE;

        vec3 rel = g_ro + g_rd * t - g_center;
        float h = length(rel);

        if (h < g_inner || h > g_outer) {
            t += coarse ? dt_coarse : dt_fine;
            continue;
        }

        float shell_frac = (h - g_inner) / g_thickness;

        if (coarse) {
            if (sample_density(rel, shell_frac, true) > 0.0) {
                coarse = false;
                empty_run = 0;
                t = max(t - dt_coarse, seg_start);
                continue;
            }
            t += dt_coarse;
            continue;
        }

        float density = sample_density(rel, shell_frac, false);

        if (density <= 0.0) {
            if (++empty_run > EMPTY_RUN_LIMIT) {
                coarse = true;
                empty_run = 0;
            }
            t += dt_fine;
            continue;
        }
        empty_run = 0;

        float light_optical_depth = light_march(rel);
        float scatter = multi_scatter(light_optical_depth, g_cos_angle);

        float powder = 1.0 - exp(-density * 2.0);
        powder = mix(1.0, powder, clamp(-g_cos_angle * 0.5 + 0.5, 0.0, 1.0));

        vec3 source = g_sun_energy * scatter * powder
                    + g_ambient_energy * mix(AMBIENT_BASE_MUL, 1.0, shell_frac);

        float step_transmittance = exp(-density * dt_fine);
        g_accumulated += g_transmittance * source * (1.0 - step_transmittance);
        g_transmittance *= step_transmittance;

        if (g_transmittance < 0.01) {
            g_transmittance = 0.0;
            return;
        }

        t += dt_fine;
    }
}

vec4 compute_clouds(ivec2 px) {
    const vec4 NO_CLOUD = vec4(0.0, 0.0, 0.0, 1.0);

    vec2 uv = (vec2(px) + 0.5) * p.march_size.zw;
    vec2 ndc = uv * 2.0 - 1.0;

    vec4 target = m.inv_projection * vec4(ndc, 1.0, 1.0);
    vec3 view_rd = normalize(target.xyz / target.w);

    g_rd = normalize((m.cam_transform * vec4(view_rd, 0.0)).xyz);
    g_ro = p.cam_pos.xyz;
    g_center = p.planet_center.xyz;
    g_inner = p.shell.x;
    g_outer = p.shell.y;
    g_thickness = g_outer - g_inner;
    g_sun_dir = normalize(p.sun.xyz);
    g_cos_angle = dot(g_rd, g_sun_dir);
    g_sun_energy = SUN_TINT * p.sun.w;
    g_ambient_energy = AMBIENT_TINT * AMBIENT_ENERGY;
    g_jitter = ign(vec2(px));
    g_accumulated = vec3(0.0);
    g_transmittance = 1.0;

    // Opaque
    ivec2 depth_px = ivec2((vec2(px) + 0.5) * p.march_size.zw * p.screen_size.xy);
    depth_px = clamp(depth_px, ivec2(0), ivec2(p.screen_size.xy) - 1);

    float t_max = p.quality.z;
    float raw_depth = texelFetch(depth_sampler, depth_px, 0).r;
    if (raw_depth > 0.0) {
        vec4 view_pos = m.inv_projection * vec4(ndc, raw_depth, 1.0);
        view_pos.xyz /= view_pos.w;
        vec3 world_pos = (m.cam_transform * vec4(view_pos.xyz, 1.0)).xyz;
        t_max = min(t_max, length(world_pos - g_ro));
    }

    // Shell
    float ot0, ot1, it0, it1;

    if (!ray_sphere(g_ro, g_rd, g_center, g_outer, ot0, ot1))
        return NO_CLOUD;

    float o0 = max(ot0, 0.0);
    float o1 = min(ot1, t_max);
    if (o1 <= o0)
        return NO_CLOUD;

    bool hit_inner = ray_sphere(g_ro, g_rd, g_center, g_inner, it0, it1);

    float a0, a1, b0, b1;
    if (!hit_inner || it1 <= 0.0) {
        a0 = o0; a1 = o1;
        b0 = 0.0; b1 = 0.0;
    } else {
        float i0 = clamp(it0, o0, o1);
        float i1 = clamp(it1, o0, o1);
        a0 = o0; a1 = i0;
        b0 = i1; b1 = o1;
    }

    // Step calculation
    int base_steps = int(p.quality.x);
    int max_iter = base_steps * 3;

    float len_a = max(a1 - a0, 0.0);
    float len_b = max(b1 - b0, 0.0);
    float total_len = len_a + len_b;
    if (total_len <= 0.0)
        return NO_CLOUD;

    float dt_base = max(g_thickness / float(base_steps), total_len / float(max_iter));

    int iter_a = max(int(float(max_iter) * (len_a / total_len)), 1);
    int iter_b = max(max_iter - iter_a, 1);

    march_segment(a0, a1, dt_base, iter_a);
    march_segment(b0, b1, dt_base, iter_b);

    return vec4(g_accumulated, g_transmittance);
}

// ---------------------------------------------------------------------------
void main() {
    ivec2 px = ivec2(gl_GlobalInvocationID.xy);
    if (px.x >= int(p.march_size.x) || px.y >= int(p.march_size.y))
        return;

    imageStore(cloud_target, px, compute_clouds(px));
}

