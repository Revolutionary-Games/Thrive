#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16f, set = 0, binding = 0) uniform image2D colorImage;   // FULL res scene
layout(set = 0, binding = 1) uniform sampler2D depthSampler;        // FULL res
layout(set = 0, binding = 2) uniform sampler2D cloudHalf;           // MARCH res

layout(set = 0, binding = 3, std140) uniform Params {
    vec4 planetCenter;
    vec4 shell;
    vec4 screenSize;     // .xy FULL res,  .zw 1/FULL res
    vec4 marchSize;      // .xy MARCH res, .zw 1/MARCH res
    vec4 cameraPosition;
    vec4 sun;
    vec4 quality;
} p;

layout(push_constant, std430) uniform Matrices {
    mat4 inverseProjection;
    mat4 cameraTransform;
} m;

// Edge sharpness
const float DEPTH_REJECT = 64.0;

float DepthKey(float raw) {
    return (raw > 0.0) ? 1.0 / raw : 1e7;
}

void main() {
    ivec2 px = ivec2(gl_GlobalInvocationID.xy);
    if (px.x >= int(p.screenSize.x) || px.y >= int(p.screenSize.y))
        return;

    ivec2 fullMax = ivec2(p.screenSize.xy) - 1;
    ivec2 marchMax = ivec2(p.marchSize.xy) - 1;

    float key = DepthKey(texelFetch(depthSampler, px, 0).r);

    vec2 uv = (vec2(px) + 0.5) * p.screenSize.zw;
    vec2 hf = uv * p.marchSize.xy - 0.5;
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
    float weightSum = 0.0;
    float bestWeight = -1.0;
    vec4 bestSample = vec4(0.0, 0.0, 0.0, 1.0);

    for (int i = 0; i < 4; i++) {
        ivec2 hp = clamp(h0 + offsets[i], ivec2(0), marchMax);

        // Matches clouds_march.glsl exactly!
        ivec2 src = ivec2((vec2(hp) + 0.5) * p.marchSize.zw * p.screenSize.xy);
        src = clamp(src, ivec2(0), fullMax);

        float sampleKey = DepthKey(texelFetch(depthSampler, src, 0).r);
        float dz = abs(sampleKey - key) / max(key, 1e-4);
        float w = bilinear[i] / (1e-3 + dz * dz * DEPTH_REJECT);

        vec4 c = texelFetch(cloudHalf, hp, 0);

        sum += c * w;
        weightSum += w;

        if (w > bestWeight) {
            bestWeight = w;
            bestSample = c;
        }
    }

    // If every neighbour was rejected, fall back to the single closest match rather than producing a hole.
    vec4 cloud = (weightSum > 1e-5) ? (sum / weightSum) : bestSample;

    vec3 scene = imageLoad(colorImage, px).rgb;

    // Tone mapped in godot already, just composite raw colours.
    vec3 outc = scene * cloud.a + cloud.rgb;

    imageStore(colorImage, px, vec4(outc, 1.0));
}
