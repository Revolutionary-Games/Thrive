shader_type spatial;
render_mode unshaded;

// this is 1 by default to keep the dark BGs looking nice
uniform float lightLevel = 1.0f;

uniform vec2 repeats = vec2(1.0f, 1.0f);
uniform sampler2D layer0 : source_color;
uniform sampler2D layer1 : source_color;
uniform sampler2D layer2 : source_color;
uniform sampler2D layer3 : source_color;

// The noise textures, distortion amount, and distortion speed for the distortion shader
uniform sampler2D noise_tex_1;
uniform sampler2D noise_tex_2;
uniform float distortion_factor;
uniform float distortion_speed;

const vec2 speed0 = vec2(3300.0f);
const vec2 speed1 = vec2(2550.0f);
const vec2 speed2 = vec2(1800.0f);
const vec2 speed3 = vec2(1050.0f);

varying vec2 UV3;
varying vec2 UV4;

vec3 LightInfluence(float amount)
{
    if (amount > 0.5f)
    {
        // Day
        return mix(vec3(0.75f, 0.5f, 0.5f), vec3(1.0f, 1.0f, 1.0f), 2.0f * amount - 1.0f);
    }
    else if (amount > 0.25f)
    {
        // Dawn and Dusk
        return mix(vec3(0.25f, 0.25f, 0.25f), vec3(0.75f, 0.5f, 0.5f), 4.0f * amount - 1.0f);
    }
    else
    {
        // Night
        return mix(vec3(0.052f, 0.05f, 0.17f), vec3(0.25f, 0.25f, 0.25f), 4.0f * amount);
    }
}

void vertex(){
    vec2 offset = (repeats - 1.0f) / 2.0f;
    vec2 worldPos = (INV_VIEW_MATRIX * vec4(0.0f, 0.0f, 0.0f, 1.0f)).xz;

    UV = (UV + worldPos / (speed0 * repeats)) * repeats - offset;
    UV2 = (0.12f + UV + worldPos / (speed1));
    UV3 = (0.512f + UV + worldPos / (speed2));
    UV4 = (0.05f + UV + worldPos / (speed3));
}

void fragment(){
    // Application of noise textures on parallax layers, creating distortion. 
    // Subtracting time results in a flip of the direction of distortion, and multiplying by factor amplifies speed.
    vec2 noise_uv1 = UV + TIME * distortion_speed;
    vec2 noise_uv2 = UV2 + TIME * distortion_speed * .8;
    vec2 noise_uv3 = UV3 - TIME * distortion_speed * 1.5;
    vec2 noise_uv4 = UV4 - TIME * distortion_speed;

    vec2 noise1 = vec2(texture(noise_tex_1, noise_uv1).r, texture(noise_tex_2, noise_uv1).r);
    vec2 noise2 = vec2(texture(noise_tex_1, noise_uv2).r, texture(noise_tex_2, noise_uv2).r);
    vec2 noise3 = vec2(texture(noise_tex_1, noise_uv3).r, texture(noise_tex_2, noise_uv3).r);
    vec2 noise4 = vec2(texture(noise_tex_1, noise_uv4).r, texture(noise_tex_2, noise_uv4).r);

    vec2 distorted_uv0 = UV + noise1 * distortion_factor;
    vec2 distorted_uv1 = UV2 + noise2 * distortion_factor;
    vec2 distorted_uv2 = UV3 + noise3 * distortion_factor;
    vec2 distorted_uv3 = UV4 + noise4 * distortion_factor;

    vec4 colour0 = texture(layer0, distorted_uv0);
    vec4 colour1 = texture(layer1, distorted_uv1);
    vec4 colour2 = texture(layer2, distorted_uv2);
    vec4 colour3 = texture(layer3, distorted_uv3);

    vec3 mixture0 = mix(colour1.rgb, colour2.rgb * colour2.a, 1.0f);
    vec3 mixture1 = mix(colour2.rgb, colour3.rgb * colour3.a, 1.0f);
    vec3 mixture2 = mix(mixture0.rgb, mixture1.rgb, 0.5f);
    vec3 composition = mix(colour0.rgb, mixture2.rgb, 0.5f);

    ALBEDO.rgb = composition.rgb * LightInfluence(lightLevel);
    ALPHA = 1.0f;
}
