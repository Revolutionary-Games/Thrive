#[compute]
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
vec3  gRo;
vec3  gRd;
vec3  gCenter;
vec3  gSunDirection;
vec3  gSunEnergy;
vec3  gAmbientEnergy;
float gInner;
float gOuter;
float gThickness;
float gCosAngle;
float gJitter;

vec3  gAccumulated;
float gTransmittance;

// ---------------------------------------------------------------------------
// Utility
// ---------------------------------------------------------------------------

// Numerically stable ray/sphere. The (|oc|-r)(|oc|+r) form avoids the catastrophic cancellation of dot(oc,oc) - r*r at
// planetary radii.
bool RaySphere(vec3 ro, vec3 rd, vec3 center, float radius, out float t0, out float t1) {
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

float Remap(float v, float inMin, float inMax, float outMin, float outMax) {
    return outMin + (v - inMin) / (inMax - inMin) * (outMax - outMin);
}

// Interleaved gradient noise
float Ign(vec2 q) {
    return fract(52.9829189 * fract(dot(q, vec2(0.06711056, 0.00583715))));
}

float HenyeyGreenstein(float cosAngle, float g) {
    float g2 = g * g;
    return (1.0 - g2) / (4.0 * PI * pow(max(1.0 + g2 - 2.0 * g * cosAngle, 1e-4), 1.5));
}

// Dual-lobe: forward scatter for silverlining.
float DualLobe(float cosAngle, float g) {
    return mix(HenyeyGreenstein(cosAngle, g),
               HenyeyGreenstein(cosAngle, -g * PHASE_BACK_G),
               PHASE_BACK_MIX);
}

// Wrenninge-Frostbite octave approximation of multiple scattering.
float MultiScatter(float opticalDepth, float cosAngle) {
    float a = 1.0;
    float b = 1.0;
    float c = 1.0;
    float lum = 0.0;
    for (int n = 0; n < MS_OCTAVES; n++) {
        lum += a * exp(-opticalDepth * b) * DualLobe(cosAngle, PHASE_G * c);
        a *= MS_ATTENUATION;
        b *= 0.5;
        c *= 0.5;
    }
    return lum;
}

float HeightGradient(float shellFrac) {
    return smoothstep(0.0, 0.1, shellFrac) * (1.0 - smoothstep(0.6, 1.0, shellFrac));
}

// ---------------------------------------------------------------------------
// Density field
// ---------------------------------------------------------------------------
float SampleDensity(vec3 rel, float shellFrac, bool cheap) {
    float tile = p.shell.z;

    float shape = texture(baseNoise, rel / tile).r;

    float coverage = p.quality.w;
    float density = clamp(Remap(shape, 1.0 - coverage, 1.0, 0.0, 1.0), 0.0, 1.0);
    density *= HeightGradient(shellFrac);

    if (density <= 0.0)
        return 0.0;

    if (!cheap) {
        vec4 detail = texture(baseNoise, rel * (DETAIL_FREQ / tile));
        float detailFbm = detail.g * 0.6098 + detail.b * 0.2439 + detail.a * 0.1463;
        float erosion = detailFbm * (1.0 - shellFrac);
        density = clamp(Remap(density, erosion * 0.2, 1.0, 0.0, 1.0), 0.0, 1.0);
    }

    return density * p.shell.w;
}

// Shadow ray toward the sun.
float LightMarch(vec3 rel) {
    int steps = int(p.quality.y);
    float stepLen = gThickness * LIGHT_STEP_FRAC;
    float opticalDepth = 0.0;
    vec3 lp = rel;

    for (int j = 0; j < steps; j++) {
        lp += gSunDirection * stepLen;
        float h = length(lp);
        if (h < gInner || h > gOuter)
            break;
        float sf = (h - gInner) / gThickness;
        opticalDepth += SampleDensity(lp, sf, true) * stepLen;
        stepLen *= LIGHT_STEP_GROW;
    }

    return opticalDepth;
}

// Ray march
void MarchSegment(float segStart, float segEnd, float dtBase, int maxIter) {
    if (segEnd <= segStart || gTransmittance < 0.01)
        return;

    float t = segStart + dtBase * gJitter;
    bool coarse = true;
    int emptyRun = 0;

    for (int i = 0; i < maxIter; i++) {
        if (t >= segEnd)
            break;

        float dtFine = dtBase * (1.0 + t * DISTANCE_LOD);
        float dtCoarse = dtFine * COARSE_STEP_SCALE;

        vec3 rel = gRo + gRd * t - gCenter;
        float h = length(rel);

        if (h < gInner || h > gOuter) {
            t += coarse ? dtCoarse : dtFine;
            continue;
        }

        float shellFrac = (h - gInner) / gThickness;

        if (coarse) {
            if (SampleDensity(rel, shellFrac, true) > 0.0) {
                coarse = false;
                emptyRun = 0;
                t = max(t - dtCoarse, segStart);
                continue;
            }
            t += dtCoarse;
            continue;
        }

        float density = SampleDensity(rel, shellFrac, false);

        if (density <= 0.0) {
            if (++emptyRun > EMPTY_RUN_LIMIT) {
                coarse = true;
                emptyRun = 0;
            }
            t += dtFine;
            continue;
        }
        emptyRun = 0;

        float lightOpticalDepth = LightMarch(rel);
        float scatter = MultiScatter(lightOpticalDepth, gCosAngle);

        float powder = 1.0 - exp(-density * 2.0);
        powder = mix(1.0, powder, clamp(-gCosAngle * 0.5 + 0.5, 0.0, 1.0));

        vec3 source = gSunEnergy * scatter * powder
                    + gAmbientEnergy * mix(AMBIENT_BASE_MUL, 1.0, shellFrac);

        float stepTransmittance = exp(-density * dtFine);
        gAccumulated += gTransmittance * source * (1.0 - stepTransmittance);
        gTransmittance *= stepTransmittance;

        if (gTransmittance < 0.01) {
            gTransmittance = 0.0;
            return;
        }

        t += dtFine;
    }
}

vec4 ComputeClouds(ivec2 px) {
    const vec4 NO_CLOUD = vec4(0.0, 0.0, 0.0, 1.0);

    vec2 uv = (vec2(px) + 0.5) * p.marchSize.zw;
    vec2 ndc = uv * 2.0 - 1.0;

    vec4 target = m.inverseProjection * vec4(ndc, 1.0, 1.0);
    vec3 viewRd = normalize(target.xyz / target.w);

    gRd = normalize((m.cameraTransform * vec4(viewRd, 0.0)).xyz);
    gRo = p.cameraPosition.xyz;
    gCenter = p.planetCenter.xyz;
    gInner = p.shell.x;
    gOuter = p.shell.y;
    gThickness = gOuter - gInner;
    gSunDirection = normalize(p.sun.xyz);
    gCosAngle = dot(gRd, gSunDirection);
    gSunEnergy = SUN_TINT * p.sun.w;
    gAmbientEnergy = AMBIENT_TINT * AMBIENT_ENERGY;
    gJitter = Ign(vec2(px));
    gAccumulated = vec3(0.0);
    gTransmittance = 1.0;

    // Opaque
    ivec2 depthPx = ivec2((vec2(px) + 0.5) * p.marchSize.zw * p.screenSize.xy);
    depthPx = clamp(depthPx, ivec2(0), ivec2(p.screenSize.xy) - 1);

    float tMax = p.quality.z;
    float rawDepth = texelFetch(depthSampler, depthPx, 0).r;
    if (rawDepth > 0.0) {
        vec4 viewPos = m.inverseProjection * vec4(ndc, rawDepth, 1.0);
        viewPos.xyz /= viewPos.w;
        vec3 worldPos = (m.cameraTransform * vec4(viewPos.xyz, 1.0)).xyz;
        tMax = min(tMax, length(worldPos - gRo));
    }

    // Shell
    float ot0, ot1, it0, it1;

    if (!RaySphere(gRo, gRd, gCenter, gOuter, ot0, ot1))
        return NO_CLOUD;

    float o0 = max(ot0, 0.0);
    float o1 = min(ot1, tMax);
    if (o1 <= o0)
        return NO_CLOUD;

    bool hitInner = RaySphere(gRo, gRd, gCenter, gInner, it0, it1);

    float a0, a1, b0, b1;
    if (!hitInner || it1 <= 0.0) {
        a0 = o0; a1 = o1;
        b0 = 0.0; b1 = 0.0;
    } else {
        float i0 = clamp(it0, o0, o1);
        float i1 = clamp(it1, o0, o1);
        a0 = o0; a1 = i0;
        b0 = i1; b1 = o1;
    }

    // Step calculation
    int baseSteps = int(p.quality.x);
    int maxIter = baseSteps * 3;

    float lenA = max(a1 - a0, 0.0);
    float lenB = max(b1 - b0, 0.0);
    float total_len = lenA + lenB;
    if (total_len <= 0.0)
        return NO_CLOUD;

    float dtBase = max(gThickness / float(baseSteps), total_len / float(maxIter));

    int iterA = max(int(float(maxIter) * (lenA / total_len)), 1);
    int iterB = max(maxIter - iterA, 1);

    MarchSegment(a0, a1, dtBase, iterA);
    MarchSegment(b0, b1, dtBase, iterB);

    return vec4(gAccumulated, gTransmittance);
}

// ---------------------------------------------------------------------------
void main() {
    ivec2 px = ivec2(gl_GlobalInvocationID.xy);
    if (px.x >= int(p.marchSize.x) || px.y >= int(p.marchSize.y))
        return;

    imageStore(cloudTarget, px, ComputeClouds(px));
}

