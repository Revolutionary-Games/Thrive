shader_type spatial;
render_mode depth_prepass_alpha;

uniform sampler2D albedoTexture : source_color;
uniform sampler2D normalTexture;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1);

uniform float outlineWidth;
uniform vec4 growColor : source_color;

void fragment() {
    vec4 mainTex = texture(albedoTexture, UV);
    vec4 normalMap = texture(normalTexture, UV);
    vec4 dissolveTex = texture(dissolveTexture, UV);

    float cutoff = dot(dissolveTex.rgb, vec3(0.382, 0.382, 0.382)) -
        float(-0.487 + clamp(dissolveValue, 0, 1));

    vec3 dissolveOutline = vec3(round(1.0 - float(cutoff - outlineWidth))) *
        growColor.rgb;

    ALBEDO = mainTex.rgb;
    NORMAL_MAP = normalMap.xyz;
    METALLIC = 0.9 * (((ALBEDO.r + ALBEDO.g + ALBEDO.b) / 3.0) * -1.0 + 1.0);
    ROUGHNESS = 0.85;
    ALPHA = round(cutoff) * mainTex.a;
    EMISSION = dissolveOutline;
}
