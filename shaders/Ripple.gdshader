shader_type spatial;

uniform sampler2D bubble_noise;
uniform sampler2D bubble_gradient;
uniform float smoothness : hint_range(0.0, 1.0, 0.05) = 0.5;
uniform float refraction_strength : hint_range(0.0, 1.0, 0.05) = 0.2;
varying vec3 world_pos;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float height = texture(bubble_noise, VERTEX.xz * smoothness + vec2(TIME * 0.1)).r;
    VERTEX += NORMAL * height * 0.1;
}

float fresnel_effect(vec3 normal, vec3 view) {
    float fresnel = dot(normalize(view), normalize(normal));
    fresnel = clamp(abs(fresnel), 0.0, 1.0);
    return pow(1.0 - fresnel, 2.0);
}

void fragment() {
    float fresnel = fresnel_effect(NORMAL, VIEW);
    vec2 distorted_uv = world_pos.xz + vec2(TIME * 0.2);
    distorted_uv += NORMAL.xz * refraction_strength;
    vec3 gradient_color = texture(bubble_gradient, distorted_uv).rgb;
    
    ALBEDO = mix(vec3(1.0), gradient_color, 0.5);
    METALLIC = 0.9;
    ROUGHNESS = 0.01;
    SPECULAR = 0.8;
    ALPHA = fresnel * 0.5;
}