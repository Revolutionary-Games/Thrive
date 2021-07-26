shader_type spatial;
render_mode specular_toon;

void fragment()
{
    float fresnel = sqrt(1.0 - dot(NORMAL, VIEW));

    ALBEDO = vec3(0.1, 0.3, 0.5) + (0.1 * fresnel);
    METALLIC = 0.5;
    ROUGHNESS = 0.3 * (1.0 - fresnel);
    ALPHA = 0.9;
    RIM = 0.2;
}