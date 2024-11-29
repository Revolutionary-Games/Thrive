shader_type spatial;
render_mode unshaded;

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_enable;

void fragment() {
	ALBEDO = texture(texture_albedo, SCREEN_UV).rgb;
}
