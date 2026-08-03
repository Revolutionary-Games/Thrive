#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16f, set = 0, binding = 0) uniform image2D color_image;
layout(set = 0, binding = 1) uniform sampler2D depth_sampler;
layout(set = 0, binding = 2) uniform sampler3D base_noise;

layout(push_constant, std430) uniform Params {
    mat4 inv_projection;      // reconstruct world ray
    vec4 cam_pos;             // .xyz world camera position
    vec4 planet_center;       // .xyz
    vec4 shell;               // .x = inner radius (R + h0), .y = outer radius (R + h1)
    vec4 screen_size_padded;  // padded, .zw unused.
} p;

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

const float base_scale = 0.000001;
const float detail_scale = 0.005;
const float density_multiplier = 1.0;
const vec3 wind = vec3(1.0, 0.0, 0.0);
const float time = 0.0;

float height_gradient(float shell_frac) {
    return smoothstep(0.0, 0.1, shell_frac) * smoothstep(1.0, 0.6, shell_frac);
}

float remap(float v, float inMin, float inMax, float outMin, float outMax) {
    return outMin + (v - inMin) / (inMax - inMin) * (outMax - outMin);
}

void main() {
    ivec2 px = ivec2(gl_GlobalInvocationID.xy);
    if (px.x >= int(p.screen_size_padded.x) || px.y >= int(p.screen_size_padded.y))
        return;

    vec2 screen_size = p.screen_size_padded.xy;
    vec2 uv = (vec2(px) + 0.5) / screen_size;
    vec2 ndc = uv * 2.0 - 1.0;
    vec4 near_h = p.inv_projection * vec4(ndc, 0.0, 1.0);
    vec4 far_h  = p.inv_projection * vec4(ndc, 1.0, 1.0);
    vec3 world_near = near_h.xyz / near_h.w;
    vec3 world_far  = far_h.xyz  / far_h.w;
    vec3 rd = normalize(world_far - world_near);
    vec3 ro = p.cam_pos.xyz;
    vec3 center = p.planet_center.xyz;

    float outer = p.shell.y;
    float inner = p.shell.x;

    // Convert to a world-space t_max along rd. For M0 you can start with t_max = large
    // and add proper depth clamping once the ray reconstruction is confirmed correct.
    float t_max = 1e9;
    float raw_depth = texelFetch(depth_sampler, px, 0).r;
    if (raw_depth > 0.0) {
        vec4 clip = vec4(ndc, raw_depth, 1.0);
        vec4 world_h = p.inv_projection * clip;
        vec3 world_pos = world_h.xyz / world_h.w;
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
    const int STEPS = 48;
    float dt = (march_end - march_start) / float(STEPS);
    float t = march_start;
    float transmittance = 1.0;

    for (int i = 0; i < STEPS; i++) {
        vec3 pos = ro + rd * t;
        float h = length(pos - p.planet_center.xyz);
        float shell_frac = clamp((h - inner) / (outer - inner), 0.0, 1.0);
        vec3 sample_pos = pos * base_scale + wind * time;
        vec4 base = texture(base_noise, sample_pos);
        float shape = base.r;

        // low-freq erosion FBM from GBA
        float base_fbm = base.g * 0.625 + base.b * 0.25 + base.a * 0.125;

        float hg = height_gradient(shell_frac);
        float coverage = 0.3; // uniform for now, weather-map channel later

        float density = clamp(remap(shape, 1.0 - coverage, 1.0, 0.0, 1.0), 0.0, 1.0);
        density *= hg;

        /*
        vec3 detail_pos = pos * detail_scale + wind * time;
        float detail = texture(detail_noise, detail_pos).r;
        float erosion = detail * (1.0 - shell_frac);
        density = clamp(remap(density, erosion * 0.2, 1.0, 0.0, 1.0), 0.0, 1.0);
        */

        density *= density_multiplier;

        // Beer-Lambert
        float extinction = density * dt;
        transmittance *= exp(-extinction);
        if (transmittance < 0.01)
            break;
        t += dt;
    }

    // Grey cloud
    float cloud_alpha = 1.0 - transmittance;
    vec3 cloud_color = vec3(0.8);
    vec4 scene = imageLoad(color_image, px);
    vec3 outc = mix(scene.rgb, cloud_color, cloud_alpha);
    imageStore(color_image, px, vec4(outc, scene.a));
}