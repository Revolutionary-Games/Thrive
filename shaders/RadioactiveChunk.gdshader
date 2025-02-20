shader_type spatial;
render_mode depth_prepass_alpha;

uniform sampler2D albedoTexture : source_color;
uniform sampler2D normalTexture;

uniform float roughness = 0.85f;
uniform float metallic = 0.15f;

uniform sampler2D emissionTexture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 emission : source_color = vec4(0.08f, 0.08f, 0.08f, 0.2f);
uniform float emissionEnergy : hint_range(0.0f, 50.0f, 0.01f) = 1.0f;
uniform bool useTextureEmission = false;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0.0f, 1.0f) = 0.0f;
uniform float dissolveScale : hint_range(0.5f, 5.0f, 0.1f) = 1.0f;

uniform float outlineWidth = 0.0f;
uniform vec4 growColor : source_color = vec4(0.0f, 0.0f, 0.0f, 1.0f);

void fragment() {
    vec4 fullTex = texture(albedoTexture, UV);

    vec4 normalMap = texture(normalTexture, UV);

    vec4 emitTex = texture(emissionTexture, UV);

    // Handle dissolve animation
    vec4 dissolveTex = texture(dissolveTexture, (UV * dissolveScale));

    float cutoff = dot(dissolveTex.rgb, vec3(0.3f, 0.3f, 0.3f)) -
        float(-0.5f + dissolveValue);

    vec3 dissolveOutline = clamp(vec3(round(1.0f - float(cutoff - outlineWidth))) *
        growColor.rgb, 0.0f, dissolveValue);

    ALBEDO = fullTex.rgb;
    NORMAL_MAP = normalMap.xyz;
    METALLIC = metallic;
    ROUGHNESS = roughness;
    ALPHA = round(cutoff) * fullTex.a;

    vec3 pixelEmission = vec3(1.0f, 1.0f, 1.0f);

    if (useTextureEmission)
    {
        vec3 emissionTex = texture(emissionTexture, UV).rgb;
        pixelEmission = emissionTex * 10.0f;
    }
    EMISSION = dissolveOutline + (emission.rgb * pixelEmission * emissionEnergy);
}
