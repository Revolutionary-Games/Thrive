shader_type texture_blit;
render_mode blend_mix;

uniform sampler2D source_texture0 : hint_blit_source0;
uniform sampler2D source_texture1 : hint_blit_source1;
uniform sampler2D source_texture2 : hint_blit_source2;
uniform sampler2D source_texture3 : hint_blit_source3;

uniform float jumpSize;

void blit() {
	COLOR0 = texture(source_texture0, UV) * MODULATE;
	
	if (COLOR0 == vec4(0.0f, 0.0f, 0.0f, 1.0f))
		COLOR0 = texture(source_texture0, UV + vec2(-jumpSize, 0.0f)) * MODULATE;
	
	if (COLOR0 == vec4(0.0f, 0.0f, 0.0f, 1.0f))
		COLOR0 = texture(source_texture0, UV + vec2(jumpSize, 0.0f)) * MODULATE;
	
	if (COLOR0 == vec4(0.0f, 0.0f, 0.0f, 1.0f))
		COLOR0 = texture(source_texture0, UV + vec2(0.0f, -jumpSize)) * MODULATE;
	
	if (COLOR0 == vec4(0.0f, 0.0f, 0.0f, 1.0f))
		COLOR0 = texture(source_texture0, UV + vec2(0.0f, jumpSize)) * MODULATE;
}
