const float DETAIL_FREQ = 8.0; // detail tile = base tile / DETAIL_FREQ

float HeightGradient(float shellFrac) {
    return smoothstep(0.0, 0.1, shellFrac) * (1.0 - smoothstep(0.6, 1.0, shellFrac));
}

float SampleDensity(vec3 rel, float shellFrac, bool cheap) {
    float tile = CLOUD_TILE;

    float shape = texture(CLOUD_NOISE, rel / tile).r;

    float coverage = CLOUD_COVERAGE;
    float density = clamp(Remap(shape, 1.0 - coverage, 1.0, 0.0, 1.0), 0.0, 1.0);
    density *= HeightGradient(shellFrac);

    if (density <= 0.0)
        return 0.0;

    if (!cheap) {
        vec4 detail = texture(CLOUD_NOISE, rel * (DETAIL_FREQ / tile));
        float detailFbm = detail.g * 0.6098 + detail.b * 0.2439 + detail.a * 0.1463;
        float erosion = detailFbm * (1.0 - shellFrac);
        density = clamp(Remap(density, erosion * 0.2, 1.0, 0.0, 1.0), 0.0, 1.0);
    }

    return density * CLOUD_DENSITY_MULTIPLIER;
}
