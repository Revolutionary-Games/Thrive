shader_type sky;

// TODO: LUTs.
// uniform sampler2D sky_view_lut : filter_linear, repeat_disable, hint_default_black;
// uniform sampler2D transmittance_lut : filter_linear, repeat_disable, hint_default_white;

uniform vec3 planet_center = vec3(0.0);
uniform vec3 sun_direction = vec3(0.4, 0.8, 0.3);
uniform float ground_radius = 2000.0;
uniform float top_radius = 2100.0;
uniform float sun_illuminance = 25.0;
uniform float sun_angular_radius : hint_range(0.0, 0.1) = 0.00465;
uniform float sun_limb_darkening : hint_range(0.0, 1.0) = 0.6;

// Rayleigh scattering coefficient at the ground, per world unit, one value per colour channel.
uniform vec3 rayleigh_scattering = vec3(0.00215, 0.00503, 0.01227);

uniform float rayleigh_scale_height = 20.0;

uniform vec3 ozone_absorption = vec3(0.000286, 0.000828, 0.0000374);
uniform float ozone_layer_center = 57.0;
uniform float ozone_layer_width = 68.0;

uniform int view_ray_steps : hint_range(4, 128) = 32;
uniform int light_ray_steps : hint_range(2, 32) = 8;

const float SKY_PI = 3.14159265358979323846;

vec2 ray_sphere(vec3 origin, vec3 direction, float radius) {
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
bool hits_planet(vec2 ground_hit) {
    return ground_hit.y > ground_hit.x && ground_hit.x > 0.0;
}

// Relative density of the two components at a given altitude.
vec2 atmosphere_density(float altitude) {
    float rayleigh = exp(-max(altitude, 0.0) / max(rayleigh_scale_height, 1e-3));
    float ozone = max(0.0, 1.0 - abs(altitude - ozone_layer_center) / max(ozone_layer_width * 0.5, 1e-3));

    return vec2(rayleigh, ozone);
}

// Rayleigh scatters without absorbing, so its scattering coefficient doubles as its extinction coefficient. Ozone
// only absorbs.
vec3 atmosphere_extinction(vec2 density) {
    return rayleigh_scattering * density.x + ozone_absorption * density.y;
}

// How much sunlight survives the trip from the top of the atmosphere down to a point.
vec3 sun_transmittance(vec3 position, vec3 sun_dir) {
    if (hits_planet(ray_sphere(position, sun_dir, ground_radius))) {
        return vec3(0.0);
    }

    vec2 top_hit = ray_sphere(position, sun_dir, top_radius);
    float step_size = max(top_hit.y, 0.0) / float(light_ray_steps);

    vec3 optical_depth = vec3(0.0);

    for (int i = 0; i < light_ray_steps; ++i) {
        vec3 sample_position = position + sun_dir * (step_size * (float(i) + 0.5));
        float altitude = length(sample_position) - ground_radius;

        optical_depth += atmosphere_extinction(atmosphere_density(altitude)) * step_size;
    }

    return exp(-optical_depth);
}

void sky() {
    vec3 sun_dir = normalize(sun_direction);
    vec3 dir = normalize(EYEDIR);

    vec3 offset = POSITION - planet_center;
    float offset_length = length(offset);
    vec3 up = offset_length > 1e-6 ? offset / offset_length : vec3(0.0, 1.0, 0.0);

    vec3 position = up * clamp(offset_length, ground_radius + 1e-3, top_radius - 1e-3);

    vec2 ground_hit = ray_sphere(position, dir, ground_radius);
    vec2 top_hit = ray_sphere(position, dir, top_radius);

    bool intersects_ground = hits_planet(ground_hit);
    float march_distance = intersects_ground ? ground_hit.x : max(top_hit.y, 0.0);

    float step_size = march_distance / float(view_ray_steps);

    float cos_theta = dot(dir, sun_dir);
    float rayleigh_phase = 3.0 / (16.0 * SKY_PI) * (1.0 + cos_theta * cos_theta);

    vec3 luminance = vec3(0.0);
    vec3 transmittance = vec3(1.0);

    for (int i = 0; i < view_ray_steps; ++i) {
        vec3 sample_position = position + dir * (step_size * (float(i) + 0.5));
        float altitude = length(sample_position) - ground_radius;

        vec2 density = atmosphere_density(altitude);
        vec3 extinction = atmosphere_extinction(density);
        vec3 step_transmittance = exp(-extinction * step_size);

        vec3 in_scatter = rayleigh_scattering * density.x * rayleigh_phase
                * sun_transmittance(sample_position, sun_dir);

        vec3 scattered = (in_scatter - in_scatter * step_transmittance) / max(extinction, vec3(1e-9));

        luminance += transmittance * scattered;
        transmittance *= step_transmittance;
    }

    luminance *= sun_illuminance;

    // Sun disc. The accumulated view transmittance reddens and dims it near the horizon, and the ground test hides
    // it once it has set.
    if (!intersects_ground) {
        float cos_disc = cos(sun_angular_radius);

        if (cos_theta > cos_disc) {
            float edge = sqrt(clamp(1.0 - (acos(clamp(cos_theta, -1.0, 1.0)) / max(sun_angular_radius, 1e-6)),
                0.0, 1.0));
            float darkening = mix(1.0, edge, sun_limb_darkening);

            float solid_angle = 2.0 * SKY_PI * (1.0 - cos_disc);
            luminance += transmittance * sun_illuminance * darkening / max(solid_angle, 1e-6);
        }
    }

    // TODO: Mie scattering for fog, smoke and clouds, on top of the Rayleigh term above.

    COLOR = luminance;
}
