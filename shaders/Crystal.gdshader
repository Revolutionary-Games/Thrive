shader_type spatial;
render_mode blend_add, depth_draw_opaque, cull_disabled, diffuse_lambert_wrap, specular_schlick_ggx;

uniform vec4 albedo : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform sampler2D albedoTexture : source_color, filter_linear_mipmap, repeat_enable;
uniform bool useNormal = false;
uniform sampler2D normalTexture;

uniform float roughness : hint_range(0, 1) = 0.6;
uniform sampler2D roughnessTexture : hint_roughness_r, filter_linear_mipmap, repeat_enable;
uniform vec4 roughnessTextureChannel = vec4(1.0, 0.0, 0.0, 0.0);

uniform float specular : hint_range(0, 1) = 0.7;
uniform float metallic : hint_range(0, 1) = 0.57;
uniform vec4 metallicTextureChannel = vec4(1.0, 0.0, 0.0, 0.0);

uniform vec4 emission : source_color = vec4(0.1, 0.1, 0.1, 1.0);
uniform float emissionEnergy = 0.25;

uniform float clearcoat : hint_range(0, 1) = 0.3;
uniform float clearcoatRoughness : hint_range(0, 1) = 0.1;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1);
uniform float outlineWidth;
uniform vec4 growColor : source_color;

void fragment() {
    vec2 baseUV = UV;
    vec4 albedoUV = texture(albedoTexture, baseUV);
    albedoUV.rgb = mix(pow((albedoUV.rgb + vec3(0.055)) * (1.0 / (1.0 + 0.055)), vec3(2.4)),
        albedoUV.rgb * (1.0 / 12.92), lessThan(albedoUV.rgb, vec3(0.04045)));

    // Handle dissolve animation
    vec4 dissolveTex = texture(dissolveTexture, baseUV);
    float cutoff = clamp(dot(dissolveTex.rgb, vec3(0.29, 0.29, 0.29)) -
        float(-0.55 + clamp(dissolveValue, 0, 1)), 0, 1.0);
    vec3 dissolveOutline = vec3(round(1.0 - float(cutoff - outlineWidth))) *
        growColor.rgb;

    ALBEDO = albedo.rgb * albedoUV.rgb;
    ALPHA = round(cutoff) * albedo.a * albedoUV.a;
    if (useNormal == true)
    {
        vec4 normalMap = texture(normalTexture, baseUV);
        NORMAL_MAP = normalMap.xyz;
    }

    float roughnessUV = 1.0 - dot(texture(roughnessTexture, baseUV), roughnessTextureChannel);
    ROUGHNESS = roughnessUV * roughness;

    float metallicTex = dot(texture(roughnessTexture, baseUV), metallicTextureChannel);
    METALLIC = metallicTex * metallic;
    SPECULAR = metallicTex * specular;

    //float emissionAlpha = 1.0 - (texture(roughnessTexture, baseUV).a);
    //EMISSION = emissionAlpha * dissolveOutline + (emission.rgb * emissionEnergy);
    vec4 emissionTex = texture(roughnessTexture, baseUV);
    EMISSION = (dissolveOutline + (emission.rgb * emissionTex.rgb) * emissionEnergy) * emissionTex.a;
    CLEARCOAT = clearcoat;
    CLEARCOAT_ROUGHNESS = clearcoatRoughness;
}
