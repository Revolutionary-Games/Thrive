shader_type spatial;
render_mode depth_prepass_alpha, blend_add, depth_draw_opaque, diffuse_burley, cull_disabled, vertex_lighting, specular_schlick_ggx;

uniform vec4 albedo : source_color;
uniform sampler2D albedoTexture : source_color;
uniform sampler2D normalTexture;
uniform float roughness : hint_range(0, 1) = 0.5;
uniform float specular : hint_range(0, 1) = 0.4;
uniform float metallic : hint_range(0, 1) = 0.45;

uniform vec4 emission : source_color = vec4(0.1, 0.1, 0.1, 1.0);
uniform float emissionEnergy = 0.25;


void fragment() {
    vec4 colorTex = texture(albedoTexture, UV);
    vec4 normalMap = texture(normalTexture, UV);

    ALBEDO = albedo.rgb * colorTex.rgb;
    NORMAL_MAP = normalMap.xyz;
    ROUGHNESS = roughness;
    SPECULAR = specular;
    METALLIC = metallic;
    ALPHA = clamp(albedo.a * colorTex.a, 0.1, 1);
    EMISSION = emission.rgb * emissionEnergy;
}
