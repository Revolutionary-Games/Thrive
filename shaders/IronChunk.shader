shader_type spatial;
render_mode depth_draw_alpha_prepass;

uniform sampler2D texture : hint_albedo;

uniform sampler2D dissolveTexture : hint_albedo;
uniform float dissolveValue : hint_range(0, 1);

uniform float outlineWidth;
uniform vec4 growColor : hint_color;

void fragment() {
    vec4 mainTex = texture(texture, UV);
    vec4 dissolveTex = texture(dissolveTexture, UV);

    float cutoff = dot(dissolveTex.rgb, vec3(0.3, 0.3, 0.3)) -
        float(-0.8 + clamp(dissolveValue, 0, 1));

    vec3 dissolveOutline = vec3(round(1.0 - float(cutoff - outlineWidth))) *
        growColor.rgb;

    // TODO: Radioactive chunk effect

    ALBEDO = mainTex.rgb;
    ALPHA = round(cutoff) * mainTex.a;
    EMISSION = dissolveOutline;
}
