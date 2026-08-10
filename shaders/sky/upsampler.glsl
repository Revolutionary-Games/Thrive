#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16f, set = 0, binding = 0) uniform image2D color_image;   // FULL res scene
layout(set = 0, binding = 1) uniform sampler2D depth_sampler;        // FULL res
layout(set = 0, binding = 2) uniform sampler2D cloud_half;           // MARCH res

layout(set = 0, binding = 3, std140) uniform Params {
    vec4 planet_center;
    vec4 shell;
    vec4 screen_size;     // .xy FULL res,  .zw 1/FULL res
    vec4 march_size;      // .xy MARCH res, .zw 1/MARCH res
    vec4 cam_pos;
    vec4 sun;
    vec4 quality;
} p;

layout(push_constant, std430) uniform Matrices {
    mat4 inv_projection;
    mat4 cam_transform;
} m;

// Edge sharpness
const float DEPTH_REJECT = 64.0;

float depth_key(float raw) {
    return (raw > 0.0) ? 1.0 / raw : 1e7;
}

void main() {
    ivec2 px = ivec2(gl_GlobalInvocationID.xy);
    if (px.x >= int(p.screen_size.x) || px.y >= int(p.screen_size.y))
        return;

    ivec2 full_max = ivec2(p.screen_size.xy) - 1;
    ivec2 march_max = ivec2(p.march_size.xy) - 1;

    float key = depth_key(texelFetch(depth_sampler, px, 0).r);

    vec2 uv = (vec2(px) + 0.5) * p.screen_size.zw;
    vec2 hf = uv * p.march_size.xy - 0.5;
    ivec2 h0 = ivec2(floor(hf));
    vec2 f = hf - vec2(h0);

    float bilinear[4] = float[4](
        (1.0 - f.x) * (1.0 - f.y),
        f.x * (1.0 - f.y),
        (1.0 - f.x) * f.y,
        f.x * f.y);

    ivec2 offsets[4] = ivec2[4](
        ivec2(0, 0), ivec2(1, 0), ivec2(0, 1), ivec2(1, 1));

    vec4 sum = vec4(0.0);
    float weight_sum = 0.0;
    float best_weight = -1.0;
    vec4 best_sample = vec4(0.0, 0.0, 0.0, 1.0);

    for (int i = 0; i < 4; i++) {
        ivec2 hp = clamp(h0 + offsets[i], ivec2(0), march_max);

        // Matches clouds_march.glsl exactly!
        ivec2 src = ivec2((vec2(hp) + 0.5) * p.march_size.zw * p.screen_size.xy);
        src = clamp(src, ivec2(0), full_max);

        float sample_key = depth_key(texelFetch(depth_sampler, src, 0).r);
        float dz = abs(sample_key - key) / max(key, 1e-4);
        float w = bilinear[i] / (1e-3 + dz * dz * DEPTH_REJECT);

        vec4 c = texelFetch(cloud_half, hp, 0);

        sum += c * w;
        weight_sum += w;

        if (w > best_weight) {
            best_weight = w;
            best_sample = c;
        }
    }

    // If every neighbour was rejected, fall back to the single closest match rather than producing a hole.
    vec4 cloud = (weight_sum > 1e-5) ? (sum / weight_sum) : best_sample;

    vec3 scene = imageLoad(color_image, px).rgb;

    // Tone mapped in godot already, just composite raw colours.
    vec3 outc = scene * cloud.a + cloud.rgb;

    imageStore(color_image, px, vec4(outc, 1.0));
}
