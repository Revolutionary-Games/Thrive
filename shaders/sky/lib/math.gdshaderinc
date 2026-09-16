const float PI = 3.14159265359;

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
