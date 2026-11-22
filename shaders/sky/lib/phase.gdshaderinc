const float MS_ATTENUATION = 0.55; // energy falloff per octave
const float PHASE_G        = 0.8;  // forward lobe eccentricity
const float PHASE_BACK_G   = 0.25; // backward lobe as a fraction of PHASE_G
const float PHASE_BACK_MIX = 0.15; // weight of the backward lobe

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

vec3 MultiScatterPhases(float cosAngle) {
    return vec3(DualLobe(cosAngle, PHASE_G), DualLobe(cosAngle, PHASE_G * 0.5), DualLobe(cosAngle, PHASE_G * 0.25))
        * vec3(1.0, MS_ATTENUATION, MS_ATTENUATION * MS_ATTENUATION);
}

// Wrenninge-Frostbite octave approximation of multiple scattering.
float MultiScatter(float opticalDepth, vec3 phases) {
    return dot(phases, exp(-opticalDepth * vec3(1.0, 0.5, 0.25)));
}
