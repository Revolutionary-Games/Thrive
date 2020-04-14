shader_type canvas_item;
uniform int mode = 0;
void fragment() {
	vec4 px = texture(SCREEN_TEXTURE, SCREEN_UV);
	if (mode==1) { px = px.gbra; }
	if (mode==2) { px = px.grba; }
	COLOR = vec4(px.rgb,1.0);
} 