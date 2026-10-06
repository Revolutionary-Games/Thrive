vec4 ComputeClouds(ivec2 px) {
    vec2 uv = (vec2(px) + 0.5) * p.marchSize.zw;
    vec2 ndc = uv * 2.0 - 1.0;

    vec4 target = m.inverseProjection * vec4(ndc, 1.0, 1.0);
    vec3 viewRd = normalize(target.xyz / target.w);

    CloudRay ray = CreateCloudRay(p.cameraPosition.xyz,
        normalize((m.cameraTransform * vec4(viewRd, 0.0)).xyz), p.planetCenter.xyz, p.shell.x, p.shell.y,
        normalize(p.sun.xyz), p.sun.w, Ign(vec2(px)));

    // Opaque
    ivec2 depthPx = ivec2((vec2(px) + 0.5) * p.marchSize.zw * p.screenSize.xy);
    depthPx = clamp(depthPx, ivec2(0), ivec2(p.screenSize.xy) - 1);

    float tMax = CLOUD_MAX_DISTANCE;
    float rawDepth = texelFetch(depthSampler, depthPx, 0).r;
    if (rawDepth > 0.0) {
        vec4 viewPos = m.inverseProjection * vec4(ndc, rawDepth, 1.0);
        viewPos.xyz /= viewPos.w;
        vec3 worldPos = (m.cameraTransform * vec4(viewPos.xyz, 1.0)).xyz;
        tMax = min(tMax, length(worldPos - ray.ro));
    }

    return MarchClouds(ray, tMax);
}

void main() {
    ivec2 px = ivec2(gl_GlobalInvocationID.xy);
    if (px.x >= int(p.marchSize.x) || px.y >= int(p.marchSize.y))
        return;

    imageStore(cloudTarget, px, ComputeClouds(px));
}
