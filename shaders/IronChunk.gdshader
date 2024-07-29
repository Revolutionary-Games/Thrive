shader_type spatial;
render_mode depth_prepass_alpha;

uniform sampler2D fullTexture : source_color;
uniform sampler2D depletedTexture : source_color;
uniform sampler2D normalTexture;

// Increase to fade the iron out of the rock
uniform float depletion : hint_range(0, 1);

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1);

uniform float outlineWidth;
uniform vec4 growColor : source_color;

void fragment() {
    vec4 fullTex = texture(fullTexture, UV);
    vec4 depletedTex = texture(depletedTexture, UV);
    vec3 mixcolor = (fullTex.rgb * (1.f - depletion)) + (depletedTex.rgb * depletion);

    float metal = clamp(mixcolor.r - mixcolor.b, 0, 1);

    vec4 normalMap = texture(normalTexture, UV);

    // Handle dissolve animation
    vec4 dissolveTex = texture(dissolveTexture, UV);

    float cutoff = dot(dissolveTex.rgb, vec3(0.29, 0.29, 0.29)) -
        float(-0.55 + clamp(dissolveValue, 0, 1));

    vec3 dissolveOutline = vec3(round(1.0 - float(cutoff - outlineWidth))) *
        growColor.rgb;

    ALBEDO = mixcolor;
    NORMAL_MAP = normalMap.xyz;
    METALLIC = clamp(1.2f * metal, 0, 1);
    ROUGHNESS = clamp(0.85f - (metal * 1.8f), 0, 1);
    ALPHA = round(cutoff) * fullTex.a;
    EMISSION = dissolveOutline;
}
