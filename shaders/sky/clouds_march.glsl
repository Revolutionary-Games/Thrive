#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16f, set = 0, binding = 0) uniform image2D color_image;
layout(set = 0, binding = 1) uniform sampler2D depth_sampler;
layout(set = 0, binding = 2) uniform sampler3D base_noise;

layout(set = 0, binding = 3, std140) uniform Params {
    vec4 planet_center;   // .xyz center                          .w unused
    vec4 shell;           // .x inner R, .y outer R, .z tile size,.w density multiplier
    vec4 screen_size;     // .xy size,   .zw 1/size
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
const int   MS_OCTAVES        = 3;      // multiple-scattering approximation octaves
const float MS_ATTENUATION    = 0.55;   // energy falloff per octave
const float PHASE_G           = 0.8;    // forward lobe eccentricity
const float PHASE_BACK_MIX    = 0.3;    // weight of the backward lobe
const float DETAIL_FREQ       = 8.0;    // detail tile = base tile / DETAIL_FREQ
const float COARSE_STEP_SCALE = 3.0;    // empty-space skip multiplier
const int   EMPTY_RUN_LIMIT   = 8;      // fine steps of nothing before going coarse again
const float LIGHT_STEP_FRAC   = 0.06;   // first light step as a fraction of shell thickness
const float LIGHT_STEP_GROW   = 1.45;   // light steps grow exponentially (cone-ish)
const vec3  SUN_TINT          = vec3(1.0, 0.95, 0.87);
const vec3  AMBIENT_TINT      = vec3(0.42, 0.55, 0.78);
const float AMBIENT_ENERGY    = 1.2;
const float AMBIENT_BASE_MUL  = 0.25;   // ambient at cloud base vs. cloud top

const float PI = 3.14159265359;

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
               henyey_greenstein(cos_angle, -g * 0.4),
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
float light_march(vec3 rel, vec3 sun_dir, float inner, float thickness) {
    int steps = int(p.quality.y);
    float step_len = thickness * LIGHT_STEP_FRAC;
    float optical_depth = 0.0;
    vec3 lp = rel;

    for (int j = 0; j < steps; j++) {
        lp += sun_dir * step_len;
        float h = length(lp);
        if (h < inner || h > inner + thickness)
            break;
        float sf = (h - inner) / thickness;
        optical_depth += sample_density(lp, sf, true) * step_len;
        step_len *= LIGHT_STEP_GROW;
    }

    return optical_depth;
}

// ---------------------------------------------------------------------------
void main() {
    ivec2 px = ivec2(gl_GlobalInvocationID.xy);
    if (px.x >= int(p.screen_size.x) || px.y >= int(p.screen_size.y))
        return;

    vec2 uv = (vec2(px) + 0.5) * p.screen_size.zw;
    vec2 ndc = uv * 2.0 - 1.0;

    vec4 target = m.inv_projection * vec4(ndc, 1.0, 1.0);
    vec3 view_rd = normalize(target.xyz / target.w);
    vec3 rd = normalize((m.cam_transform * vec4(view_rd, 0.0)).xyz);

    vec3 ro = p.cam_pos.xyz;
    vec3 center = p.planet_center.xyz;
    float inner = p.shell.x;
    float outer = p.shell.y;
    float thickness = outer - inner;

    // Opaque
    float t_max = p.quality.z;
    float raw_depth = texelFetch(depth_sampler, px, 0).r;
    if (raw_depth > 0.0) {
        vec4 view_pos = m.inv_projection * vec4(ndc, raw_depth, 1.0);
        view_pos.xyz /= view_pos.w;
        vec3 world_pos = (m.cam_transform * vec4(view_pos.xyz, 1.0)).xyz;
        t_max = min(t_max, length(world_pos - ro));
    }

    // Shell
    float ot0, ot1, it0, it1;
    bool hit_outer = ray_sphere(ro, rd, center, outer, ot0, ot1);
    if (!hit_outer || ot1 <= 0.0)
        return;

    bool hit_inner = ray_sphere(ro, rd, center, inner, it0, it1);
    float cam_r = length(ro - center);
    float march_start, march_end;

    if (cam_r > outer) {
        march_start = ot0;
        march_end = (hit_inner && it0 > 0.0) ? it0 : ot1;
    } else if (cam_r < inner) {
        if (!hit_inner)
            return;
        march_start = it1;
        march_end = ot1;
    } else {
        march_start = 0.0;
        march_end = (hit_inner && it0 > 0.0) ? it0 : ot1;
    }

    march_start = max(march_start, 0.0);
    march_end = min(march_end, t_max);
    if (march_end <= march_start)
        return;

    // Step calculation
    int base_steps = int(p.quality.x);
    int max_iter = base_steps * 3;
    float march_len = march_end - march_start;
    float dt_fine = max(thickness / float(base_steps), march_len / float(max_iter));
    float dt_coarse = dt_fine * COARSE_STEP_SCALE;

    // Raymarch
    vec3 sun_dir = normalize(p.sun.xyz);
    float cos_angle = dot(rd, sun_dir);
    vec3 sun_energy = SUN_TINT * p.sun.w;
    vec3 ambient_energy = AMBIENT_TINT * AMBIENT_ENERGY;

    float t = march_start + dt_fine * ign(vec2(px));
    vec3 accumulated = vec3(0.0);
    float transmittance = 1.0;
    bool coarse = true;
    int empty_run = 0;

    for (int i = 0; i < max_iter; i++) {
        if (t >= march_end)
            break;

        vec3 rel = ro + rd * t - center;
        float h = length(rel);

        if (h < inner || h > outer) {
            t += coarse ? dt_coarse : dt_fine;
            continue;
        }

        float shell_frac = (h - inner) / thickness;

        if (coarse) {
            if (sample_density(rel, shell_frac, true) > 0.0) {
                coarse = false;
                empty_run = 0;
                t = max(t - dt_coarse, march_start);
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

        float light_optical_depth = light_march(rel, sun_dir, inner, thickness);
        float scatter = multi_scatter(light_optical_depth, cos_angle);

        float powder = 1.0 - exp(-density * 2.0);
        powder = mix(1.0, powder, clamp(-cos_angle * 0.5 + 0.5, 0.0, 1.0));

        vec3 source = sun_energy * scatter * powder
                    + ambient_energy * mix(AMBIENT_BASE_MUL, 1.0, shell_frac);

        float step_transmittance = exp(-density * dt_fine);
        accumulated += transmittance * source * (1.0 - step_transmittance);
        transmittance *= step_transmittance;

        if (transmittance < 0.01) {
            transmittance = 0.0;
            break;
        }

        t += dt_fine;
    }

    // Composite
    vec3 scene = imageLoad(color_image, px).rgb;
    vec3 outc = scene * transmittance + accumulated;

    imageStore(color_image, px, vec4(outc, 1.0));
}
