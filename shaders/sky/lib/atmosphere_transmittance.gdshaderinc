// Looks up the LUT baked by AtmosphereTransmittanceLut, without testing for the planet's shadow. Below the horizon this
// gives the transmittance of the grazing ray.
vec3 AtmosphereTransmittanceLookup(sampler2D lut, float pointRadius, float cosZenith, float lutGroundRadius,
    float lutTopRadius) {
    float radius = clamp(pointRadius, lutGroundRadius, lutTopRadius);

    float topHorizonDistance = sqrt((lutTopRadius - lutGroundRadius) * (lutTopRadius + lutGroundRadius));
    float pointHorizonDistance = sqrt(max((radius - lutGroundRadius) * (radius + lutGroundRadius), 0.0));

    float discriminant = radius * radius * cosZenith * cosZenith + (lutTopRadius - radius) * (lutTopRadius + radius);
    float distanceToTop = max(-radius * cosZenith + sqrt(max(discriminant, 0.0)), 0.0);

    float minDistance = lutTopRadius - radius;
    float maxDistance = pointHorizonDistance + topHorizonDistance;

    vec2 uv = vec2((distanceToTop - minDistance) / max(maxDistance - minDistance, 1e-6),
        pointHorizonDistance / topHorizonDistance);

    vec2 size = vec2(textureSize(lut, 0));
    uv = 0.5 / size + uv * (1.0 - 1.0 / size);

    return texture(lut, uv).rgb;
}

// True when the planet blocks the sun from a point.
bool InPlanetShadow(vec3 point, vec3 towardsSun, float lutGroundRadius) {
    float alongSun = dot(point, towardsSun);
    return alongSun < 0.0 && dot(point, point) - alongSun * alongSun < lutGroundRadius * lutGroundRadius;
}

// How much sunlight survives the trip from the top of the atmosphere down to a point, given relative to the planet
// center.
vec3 AtmosphereTransmittance(sampler2D lut, vec3 point, vec3 towardsSun, float lutGroundRadius, float lutTopRadius) {
    float pointRadius = length(point);
    float cosZenith = dot(point, towardsSun) / max(pointRadius, 1e-6);

    if (InPlanetShadow(point, towardsSun, lutGroundRadius)) {
        return vec3(0.0);
    }

    return AtmosphereTransmittanceLookup(lut, pointRadius, cosZenith, lutGroundRadius, lutTopRadius);
}
