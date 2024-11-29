shader_type canvas_item;
render_mode unshaded;

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_enable;

uniform vec2 blur_step = vec2(1.0f, 0.0f);

void fragment() {
	vec2 s = blur_step / 1024.0f;
	COLOR.rgb =
		0.011194727 * texture(texture_albedo, UV - 10.000000000 * s).rgb +
		0.039368696 * texture(texture_albedo, UV - 8.415809477 * s).rgb +
		0.071308558 * texture(texture_albedo, UV - 6.435363708 * s).rgb +
		0.110237219 * texture(texture_albedo, UV - 4.455121108 * s).rgb +
		0.145451038 * texture(texture_albedo, UV - 2.475020813 * s).rgb +
		0.163798995 * texture(texture_albedo, UV - 0.495000167 * s).rgb +
		0.157439298 * texture(texture_albedo, UV + 1.485004498 * s).rgb +
		0.129158204 * texture(texture_albedo, UV + 3.465057055 * s).rgb +
		0.090434685 * texture(texture_albedo, UV + 5.445220765 * s).rgb +
		0.054043977 * texture(texture_albedo, UV + 7.425557483 * s).rgb +
		0.027564604 * texture(texture_albedo, UV + 9.406126897 * s).rgb;
	
	COLOR.a = 1.0f;
}