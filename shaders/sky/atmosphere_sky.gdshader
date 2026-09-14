shader_type sky;

// TODO: LUTs.
// uniform sampler2D skyViewLut : filter_linear, repeat_disable, hint_default_black;
// uniform sampler2D transmittanceLut : filter_linear, repeat_disable, hint_default_white;

uniform vec3 planetCenter = vec3(0.0);
uniform vec3 sunDirection = vec3(0.4, 0.8, 0.3);
uniform float groundRadius = 2000.0;
uniform float topRadius = 2100.0;
uniform float sunIlluminance = 25.0;
uniform float sunAngularRadius : hint_range(0.0, 0.1) = 0.00465;
uniform float sunLimbDarkening : hint_range(0.0, 1.0) = 0.6;

// Rayleigh scattering coefficient at the ground, per world unit, one value per colour channel.
uniform vec3 rayleighScattering = vec3(0.00215, 0.00503, 0.01227);

uniform float rayleighScaleHeight = 20.0;

uniform vec3 ozoneAbsorption = vec3(0.000286, 0.000828, 0.0000374);
uniform float ozoneLayerCenter = 57.0;
uniform float ozoneLayerWidth = 68.0;

uniform int viewRaySteps : hint_range(4, 128) = 32;
uniform int lightRaySteps : hint_range(2, 32) = 8;

const float SKY_PI = 3.14159265358979323846;

vec2 raySphere(vec3 origin, vec3 direction, float radius) {
    float b = dot(origin, direction);
    float c = dot(origin, origin) - radius * radius;
    float discriminant = b * b - c;

    if (discriminant < 0.0) {
        return vec2(1.0, -1.0);
    }

    float root = sqrt(discriminant);

    return vec2(-b - root, -b + root);
}

// True when a ray starting above the ground runs into the planet.
bool hitsPlanet(vec2 groundHit) {
    return groundHit.y > groundHit.x && groundHit.x > 0.0;
}

// Relative density of the two components at a given altitude.
vec2 atmosphereDensity(float altitude) {
    float rayleigh = exp(-max(altitude, 0.0) / max(rayleighScaleHeight, 1e-3));
    float ozone = max(0.0, 1.0 - abs(altitude - ozoneLayerCenter) / max(ozoneLayerWidth * 0.5, 1e-3));

    return vec2(rayleigh, ozone);
}

// Rayleigh scatters without absorbing, so its scattering coefficient doubles as its extinction coefficient. Ozone
// only absorbs.
vec3 atmosphereExtinction(vec2 density) {
    return rayleighScattering * density.x + ozoneAbsorption * density.y;
}

// How much sunlight survives the trip from the top of the atmosphere down to a point.
vec3 sunTransmittance(vec3 position, vec3 directionToSun) {
    if (hitsPlanet(raySphere(position, directionToSun, groundRadius))) {
        return vec3(0.0);
    }

    vec2 topHit = raySphere(position, directionToSun, topRadius);
    float stepSize = max(topHit.y, 0.0) / float(lightRaySteps);

    vec3 opticalDepth = vec3(0.0);

    for (int i = 0; i < lightRaySteps; ++i) {
        vec3 samplePosition = position + directionToSun * (stepSize * (float(i) + 0.5));
        float altitude = length(samplePosition) - groundRadius;

        opticalDepth += atmosphereExtinction(atmosphereDensity(altitude)) * stepSize;
    }

    return exp(-opticalDepth);
}

void sky() {
    vec3 directionToSun = normalize(sunDirection);
    vec3 viewDirection = normalize(EYEDIR);

    vec3 offset = POSITION - planetCenter;
    float offsetLength = length(offset);
    vec3 up = offsetLength > 1e-6 ? offset / offsetLength : vec3(0.0, 1.0, 0.0);

    vec3 position = up * max(offsetLength, groundRadius + 1e-3);

    vec2 groundHit = raySphere(position, viewDirection, groundRadius);
    vec2 topHit = raySphere(position, viewDirection, topRadius);

    bool intersectsGround = hitsPlanet(groundHit);

    float marchStart = max(topHit.x, 0.0);
    float marchEnd = intersectsGround ? groundHit.x : topHit.y;
    float marchDistance = max(marchEnd - marchStart, 0.0);

    float stepSize = marchDistance / float(viewRaySteps);

    float cosTheta = dot(viewDirection, directionToSun);
    float rayleighPhase = 3.0 / (16.0 * SKY_PI) * (1.0 + cosTheta * cosTheta);

    vec3 luminance = vec3(0.0);
    vec3 transmittance = vec3(1.0);

    if (marchDistance > 0.0) {
        for (int i = 0; i < viewRaySteps; ++i) {
            vec3 samplePosition = position + viewDirection * (marchStart + stepSize * (float(i) + 0.5));
            float altitude = length(samplePosition) - groundRadius;

            vec2 density = atmosphereDensity(altitude);
            vec3 extinction = atmosphereExtinction(density);
            vec3 stepTransmittance = exp(-extinction * stepSize);

            vec3 inScatter = rayleighScattering * density.x * rayleighPhase
                    * sunTransmittance(samplePosition, directionToSun);

            vec3 scattered = (inScatter - inScatter * stepTransmittance) / max(extinction, vec3(1e-9));

            luminance += transmittance * scattered;
            transmittance *= stepTransmittance;
        }
    }

    luminance *= sunIlluminance;

    // Sun disc. The accumulated view transmittance reddens and dims it near the horizon, and the ground test hides
    // it once it has set.
    if (!intersectsGround) {
        float cosDisc = cos(sunAngularRadius);

        if (cosTheta > cosDisc) {
            float edge = sqrt(clamp(1.0 - (acos(clamp(cosTheta, -1.0, 1.0)) / max(sunAngularRadius, 1e-6)),
                0.0, 1.0));
            float darkening = mix(1.0, edge, sunLimbDarkening);

            float solidAngle = 2.0 * SKY_PI * (1.0 - cosDisc);
            luminance += transmittance * sunIlluminance * darkening / max(solidAngle, 1e-6);
        }
    }

    // TODO: Mie scattering for fog, smoke and clouds, on top of the Rayleigh term above.

    COLOR = luminance;
}
