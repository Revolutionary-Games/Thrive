shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert_wrap;

uniform vec4 albedo : source_color = vec4(1.0f, 1.0f, 1.0f, 1.0f);
uniform sampler2D albedoTexture : source_color, filter_linear_mipmap, repeat_enable;
uniform bool useNormal = false;
uniform sampler2D normalTexture;

uniform float roughness : hint_range(0, 1) = 0.6f;
uniform sampler2D roughnessTexture : hint_roughness_r, filter_linear_mipmap, repeat_enable;
uniform vec4 roughnessTextureChannel = vec4(1.0f, 0.0f, 0.0f, 0.0f);

uniform float specular : hint_range(0, 1) = 0.7f;
uniform float metallic : hint_range(0, 1) = 0.57f;

uniform vec4 emission : source_color = vec4(0.1f, 0.1f, 0.1f, 1.0f);
uniform float emissionEnergy = 0.25f;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1) = 0.0f;
uniform float dissolveScale : hint_range(0.5f, 5.0f, 0.1f) = 1.0f;

uniform float outlineWidth : hint_range(0.0f, 0.25f, 0.01f) = 0.0f;

void fragment() {
    vec4 albedoUV = texture(albedoTexture, UV);
    vec4 final =  albedo * albedoUV;

    // Handle dissolve animation
    vec4 dissolveTex = texture(dissolveTexture, (UV * dissolveScale));

    float cutoff = clamp(dot(dissolveTex.rgb, vec3(0.3f, 0.3f, 0.3f)) -
        float(dissolveValue) + 0.475f + outlineWidth, 0.0f, 1.0f);
    float innerCutoff = clamp(dot(dissolveTex.rgb, vec3(0.3f, 0.3f, 0.3f)) -
        float(dissolveValue) + 0.4f, 0.0f, 1.0f);

    float dissolveOutline = clamp((round(cutoff) - round(innerCutoff)), 0.0f, dissolveValue);

    ALBEDO = final.rgb;
    ALPHA = final.a * (round(cutoff) - dissolveOutline);
    if (useNormal == true)
    {
        vec4 normalMap = texture(normalTexture, UV);
        NORMAL_MAP = normalMap.xyz;
    }

    float roughnessUV = dot(texture(roughnessTexture, UV), roughnessTextureChannel);
    ROUGHNESS = roughnessUV * roughness;

    METALLIC = metallic;
    SPECULAR = specular;

    EMISSION = emission.rgb * roughnessUV * emissionEnergy;
}
