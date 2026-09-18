void vertex() {
    POSITION = vec4(VERTEX.xy, 0.0, 1.0);
}

void fragment() {
    vec2 ndc = SCREEN_UV * 2.0 - 1.0;

    vec4 target = INV_PROJECTION_MATRIX * vec4(ndc, 1.0, 1.0);
    vec3 viewRd = normalize(target.xyz / target.w);

    CloudRay ray = CreateCloudRay(CAMERA_POSITION_WORLD, normalize((INV_VIEW_MATRIX * vec4(viewRd, 0.0)).xyz),
        cloudPlanetCenter, cloudInnerRadius, cloudOuterRadius, normalize(cloudSunDirection), cloudSunEnergy,
        Ign(FRAGCOORD.xy));

    // Opaque. OpenGL has no reversed Z, so nothing drawn means a depth of 1 and NDC depth goes from -1 to 1. The
    // RenderingDevice renderers use reversed Z, where nothing drawn is 0 and NDC depth is already 0 to 1.
    float tMax = CLOUD_MAX_DISTANCE;
    float rawDepth = texture(depthTexture, SCREEN_UV).r;
#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
    bool hitOpaque = rawDepth < 1.0;
    float ndcDepth = rawDepth * 2.0 - 1.0;
#else
    bool hitOpaque = rawDepth > 0.0;
    float ndcDepth = rawDepth;
#endif
    if (hitOpaque) {
        vec4 viewPos = INV_PROJECTION_MATRIX * vec4(ndc, ndcDepth, 1.0);
        viewPos.xyz /= viewPos.w;
        tMax = min(tMax, length(viewPos.xyz));
    }

    vec4 cloud = MarchClouds(ray, tMax);

    // Premultiplied blending gives scene * transmittance + scattered light, same as the compute upsampler
    ALBEDO = cloud.rgb;
    ALPHA = 1.0 - cloud.a;
}
