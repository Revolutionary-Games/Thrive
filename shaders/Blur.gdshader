shader_type canvas_item;
render_mode unshaded;

uniform sampler2D textureAlbedo : source_color, filter_linear_mipmap, repeat_enable;

uniform vec2 blurStep = vec2(1.0f, 0.0f);

void fragment() {
	vec2 s = blurStep / vec2(textureSize(textureAlbedo, 0));
	COLOR.rgb =
		0.011194727 * texture(textureAlbedo, UV - 10.000000000 * s).rgb +
		0.039368696 * texture(textureAlbedo, UV - 8.415809477 * s).rgb +
		0.071308558 * texture(textureAlbedo, UV - 6.435363708 * s).rgb +
		0.110237219 * texture(textureAlbedo, UV - 4.455121108 * s).rgb +
		0.145451038 * texture(textureAlbedo, UV - 2.475020813 * s).rgb +
		0.163798995 * texture(textureAlbedo, UV - 0.495000167 * s).rgb +
		0.157439298 * texture(textureAlbedo, UV + 1.485004498 * s).rgb +
		0.129158204 * texture(textureAlbedo, UV + 3.465057055 * s).rgb +
		0.090434685 * texture(textureAlbedo, UV + 5.445220765 * s).rgb +
		0.054043977 * texture(textureAlbedo, UV + 7.425557483 * s).rgb +
		0.027564604 * texture(textureAlbedo, UV + 9.406126897 * s).rgb;
	
	COLOR.a = 1.0f;
}