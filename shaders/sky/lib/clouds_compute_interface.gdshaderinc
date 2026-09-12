#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16f, set = 0, binding = 0) uniform image2D cloudTarget;
layout(set = 0, binding = 1) uniform sampler2D depthSampler;   // FULL resolution
layout(set = 0, binding = 2) uniform sampler3D baseNoise;

layout(set = 0, binding = 3, std140) uniform Params {
    vec4 planetCenter;   // .xyz center                          .w unused
    vec4 shell;          // .x inner R, .y outer R, .z tile size,.w density multiplier
    vec4 screenSize;     // .xy size,   .zw 1/size
    vec4 marchSize;      // .xy MARCH res, .zw 1/MARCH res
    vec4 cameraPosition; // .xyz world camera position           .w unused
    vec4 sun;            // .xyz direction TOWARD sun (unit)     .w sun energy
    vec4 quality;        // .x base steps, .y light steps, .z max march dist, .w coverage
} p;

layout(push_constant, std430) uniform Matrices {
    mat4 inverseProjection;
    mat4 cameraTransform;
} m;

// The shared modules read their parameters only through these, so a backend is free to store them however it wants.
// A gdshader backend points them at its own uniforms instead of this UBO.
#define CLOUD_NOISE baseNoise
#define CLOUD_TILE p.shell.z
#define CLOUD_DENSITY_MULTIPLIER p.shell.w
#define CLOUD_COVERAGE p.quality.w
#define CLOUD_BASE_STEPS int(p.quality.x)
#define CLOUD_LIGHT_STEPS int(p.quality.y)
#define CLOUD_MAX_DISTANCE p.quality.z
