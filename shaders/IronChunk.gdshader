shader_type spatial;
render_mode depth_prepass_alpha;

uniform sampler2D fullTexture : source_color;
uniform sampler2D depletedTexture : source_color;
uniform sampler2D normalTexture;

uniform float roughness = 0.85f;
uniform vec4 metallicTextureChannel = vec4(1.0f, 0.0f, 0.0f, 0.0f);

// Increase to fade the iron out of the rock
uniform float depletion : hint_range(0, 1);

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1);
uniform float outlineWidth;
uniform vec4 growColor : source_color;

void fragment() {
    vec4 fullTex = texture(fullTexture, UV);
    vec4 depletedTex = texture(depletedTexture, UV);
    vec3 mixcolor = (fullTex.rgb * (1.0f - depletion)) + (depletedTex.rgb *
        depletion);

    float metal = dot(fullTex, metallicTextureChannel);

    vec4 normalMap = texture(normalTexture, UV);

    // Handle dissolve animation
    vec4 dissolveTex = texture(dissolveTexture, UV);
    float cutoff = dot(dissolveTex.rgb, vec3(0.3f, 0.3f, 0.3f)) -
        float(-0.5f + clamp(dissolveValue, 0.0f, 1.0f));
    vec3 dissolveOutline = vec3(round(1.0f - float(cutoff - outlineWidth))) *
        growColor.rgb;

    ALBEDO = mixcolor;
    NORMAL_MAP = normalMap.xyz;
    METALLIC = clamp((2.5f * metal) + 0.2f, 0.0f, 1.0f);
    ROUGHNESS = clamp((0.5f + roughness/2.0f) - (metal * 1.5f), 0.0f, 1.0f);
    ALPHA = round(cutoff) * fullTex.a;
    EMISSION = dissolveOutline;
}
