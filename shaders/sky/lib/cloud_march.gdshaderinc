const float COARSE_STEP_SCALE = 3.0;     // empty-space skip multiplier
const int   EMPTY_RUN_LIMIT   = 8;       // fine steps of nothing before going coarse again
const float LIGHT_STEP_FRAC   = 0.06;    // first light step as a fraction of shell thickness
const float LIGHT_STEP_GROW   = 1.45;    // light steps grow exponentially (cone-ish)
const vec3  SUN_TINT          = vec3(1.0, 0.95, 0.87);
const vec3  AMBIENT_TINT      = vec3(0.42, 0.55, 0.78);
const float AMBIENT_ENERGY    = 1.2;
const float AMBIENT_BASE_MUL  = 0.25;    // ambient at cloud base vs. cloud top
const float DISTANCE_LOD      = 0.00015; // step growth per unit of distance

const vec4 NO_CLOUD = vec4(0.0, 0.0, 0.0, 1.0);

struct CloudRay {
    vec3  ro;
    vec3  rd;
    vec3  center;
    vec3  sunDirection;
    vec3  sunEnergy;
    vec3  ambientEnergy;
    float inner;
    float outer;
    float thickness;
    float cosAngle;
    float jitter;

    vec3  accumulated;
    float transmittance;
};

CloudRay CreateCloudRay(vec3 ro, vec3 rd, vec3 center, float inner, float outer, vec3 sunDirection, float sunEnergy,
    float jitter) {
    CloudRay ray;
    ray.ro = ro;
    ray.rd = rd;
    ray.center = center;
    ray.sunDirection = sunDirection;
    ray.sunEnergy = SUN_TINT * sunEnergy;
    ray.ambientEnergy = AMBIENT_TINT * AMBIENT_ENERGY;
    ray.inner = inner;
    ray.outer = outer;
    ray.thickness = outer - inner;
    ray.cosAngle = dot(rd, sunDirection);
    ray.jitter = jitter;
    ray.accumulated = vec3(0.0);
    ray.transmittance = 1.0;
    return ray;
}

// Shadow ray toward the sun.
float LightMarch(CloudRay ray, vec3 rel) {
    int steps = CLOUD_LIGHT_STEPS;
    float stepLen = ray.thickness * LIGHT_STEP_FRAC;
    float opticalDepth = 0.0;
    vec3 lp = rel;

    for (int j = 0; j < steps; j++) {
        lp += ray.sunDirection * stepLen;
        float h = length(lp);
        if (h < ray.inner || h > ray.outer)
            break;
        float sf = (h - ray.inner) / ray.thickness;
        opticalDepth += SampleDensity(lp, sf, true) * stepLen;
        stepLen *= LIGHT_STEP_GROW;
    }

    return opticalDepth;
}

// Ray march
void MarchSegment(inout CloudRay ray, float segStart, float segEnd, float dtBase, int maxIter) {
    if (segEnd <= segStart || ray.transmittance < 0.01)
        return;

    float t = segStart + dtBase * ray.jitter;
    bool coarse = true;
    int emptyRun = 0;

    for (int i = 0; i < maxIter; i++) {
        if (t >= segEnd)
            break;

        float dtFine = dtBase * (1.0 + t * DISTANCE_LOD);
        float dtCoarse = dtFine * COARSE_STEP_SCALE;

        vec3 rel = ray.ro + ray.rd * t - ray.center;
        float h = length(rel);

        if (h < ray.inner || h > ray.outer) {
            t += coarse ? dtCoarse : dtFine;
            continue;
        }

        float shellFrac = (h - ray.inner) / ray.thickness;

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

        float lightOpticalDepth = LightMarch(ray, rel);
        float scatter = MultiScatter(lightOpticalDepth, ray.cosAngle);

        float powder = 1.0 - exp(-density * 2.0);
        powder = mix(1.0, powder, clamp(-ray.cosAngle * 0.5 + 0.5, 0.0, 1.0));

        vec3 source = ray.sunEnergy * scatter * powder
                    + ray.ambientEnergy * mix(AMBIENT_BASE_MUL, 1.0, shellFrac);

        float stepTransmittance = exp(-density * dtFine);
        ray.accumulated += ray.transmittance * source * (1.0 - stepTransmittance);
        ray.transmittance *= stepTransmittance;

        if (ray.transmittance < 0.01) {
            ray.transmittance = 0.0;
            return;
        }

        t += dtFine;
    }
}

// Marches an already set up ray through the cloud shell, stopping at tMax. Scattered light ends up in .rgb and the
// remaining transmittance in .a.
vec4 MarchClouds(inout CloudRay ray, float tMax) {
    float ot0, ot1, it0, it1;

    if (!RaySphere(ray.ro, ray.rd, ray.center, ray.outer, ot0, ot1))
        return NO_CLOUD;

    float o0 = max(ot0, 0.0);
    float o1 = min(ot1, tMax);
    if (o1 <= o0)
        return NO_CLOUD;

    bool hitInner = RaySphere(ray.ro, ray.rd, ray.center, ray.inner, it0, it1);

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
    int baseSteps = CLOUD_BASE_STEPS;
    int maxIter = baseSteps * 3;

    float lenA = max(a1 - a0, 0.0);
    float lenB = max(b1 - b0, 0.0);
    float totalLength = lenA + lenB;
    if (totalLength <= 0.0)
        return NO_CLOUD;

    float dtBase = max(ray.thickness / float(baseSteps), totalLength / float(maxIter));

    int iterA = max(int(float(maxIter) * (lenA / totalLength)), 1);
    int iterB = max(maxIter - iterA, 1);

    MarchSegment(ray, a0, a1, dtBase, iterA);
    MarchSegment(ray, b0, b1, dtBase, iterB);

    return vec4(ray.accumulated, ray.transmittance);
}
