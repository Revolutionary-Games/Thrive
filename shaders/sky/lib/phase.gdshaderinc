const int   MS_OCTAVES     = 3;    // multiple-scattering approximation octaves
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
