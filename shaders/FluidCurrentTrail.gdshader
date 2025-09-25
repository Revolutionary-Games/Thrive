shader_type spatial;
render_mode blend_mix, depth_draw_opaque, particle_trails;

void fragment() {
    float uvR = (UV.r * 2.0f - 1.0f);
    vec3 normal = normalize(BINORMAL * vec3(-1.0f, -1.0f, 0.0f)) * uvR
        + vec3(0.0f, 0.0f, 1.0f) * (1.0f - abs(uvR));
    NORMAL = normal;

    ALBEDO = COLOR.rgb;
    ALPHA = (1.0f - abs(uvR)) * COLOR.a;

    SPECULAR = 1.0f;
    ROUGHNESS = 0.4f;
    METALLIC = 0.0f;
}
