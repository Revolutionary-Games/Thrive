shader_type spatial;
render_mode depth_prepass_alpha;

uniform sampler2D albedoTexture : source_color;
uniform sampler2D normalTexture;

uniform float roughness = 0.85f;
uniform float metallic = 0.85f;

uniform sampler2D emissionTexture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 emission : source_color = vec4(0.08f, 0.08f, 0.08f, 1.f);
uniform float emissionEnergy : hint_range(0.0, 50.0, 0.01) = 1.f;
uniform bool useTextureEmission = false;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1) = 0.0f;
uniform float dissolveScale : hint_range(0.5, 5.0, 0.1) = 1.f;

uniform float outlineWidth = 0.0f;
uniform vec4 growColor : source_color = vec4(0.0f, 0.0f, 0.0f, 1.0f);

void fragment() {
    vec4 fullTex = texture(albedoTexture, UV);

    vec4 normalMap = texture(normalTexture, UV);

    vec4 emitTex = texture(emissionTexture, UV);

    // Handle dissolve animation
    vec4 dissolveTex = texture(dissolveTexture, (UV * dissolveScale));

    float cutoff = dot(dissolveTex.rgb, vec3(0.3, 0.3, 0.3)) -
        float(-0.5 + dissolveValue);

    vec3 dissolveOutline = clamp(vec3(round(1.0 - float(cutoff - outlineWidth))) *
        growColor.rgb, 0, dissolveValue);

    ALBEDO = fullTex.rgb;
    NORMAL_MAP = normalMap.xyz;
    METALLIC = metallic;
    ROUGHNESS = roughness;
    ALPHA = round(cutoff) * fullTex.a;
    // EMISSION = clamp(dissolveOutline, 0.f, dissolveValue);

    vec3 pixelEmission = vec3(1.f, 1.f, 1.f);

    if (useTextureEmission)
    {
        vec3 emissionTex = texture(emissionTexture, UV).rgb;
        pixelEmission = emissionTex * 10.f;
    }
    EMISSION = dissolveOutline + (emission.rgb * pixelEmission * emissionEnergy);
}
