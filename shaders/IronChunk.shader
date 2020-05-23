shader_type spatial;
render_mode depth_draw_always;

uniform sampler2D texture : hint_albedo;

uniform sampler2D dissolveTexture : hint_albedo;
uniform float dissolveValue : hint_range(0, 1);

uniform float outlineWidth;
uniform vec4 growColor : hint_color;

void fragment() {
	vec4 mainTex = texture(texture, UV);
	vec4 dissolveTex = texture(dissolveTexture, UV);

	float alpha = dot(dissolveTex.rgb, vec3(0.3, 0.3, 0.3)) -
		float(-0.8 + dissolveValue);

	vec3 emission = vec3(round(1.0 - float(alpha - outlineWidth))) *
		growColor.rgb;

	ALBEDO = mainTex.rgb;
	ALPHA = round(alpha);
	EMISSION = emission;
}