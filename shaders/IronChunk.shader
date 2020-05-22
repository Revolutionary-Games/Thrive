shader_type spatial;
render_mode depth_draw_always;

uniform sampler2D Texture : hint_albedo;

uniform sampler2D DissolveTexture : hint_albedo;
uniform float DissolveValue : hint_range(0, 1);

uniform float OutlineWidth;
uniform vec4 GrowColor : hint_color;

void fragment() {
	vec4 mainTex = texture(Texture, UV);
	vec4 dissolveTex = texture(DissolveTexture, UV);

	float alpha = dot(dissolveTex.rgb, vec3(0.3, 0.3, 0.3)) -
		float(-0.8 + DissolveValue);

	vec3 emission = vec3(round(1.0 - float(alpha - OutlineWidth))) *
		GrowColor.rgb;

	ALBEDO = mainTex.rgb;
	ALPHA = round(alpha);
	EMISSION = emission;
}