shader_type canvas_item;

uniform vec4 fg_col;
uniform vec4 bg_col;

void fragment() {
    vec4 screen_col = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0);
    float alpha = texture(TEXTURE, UV).a;
    if ((fg_col - screen_col).x < 0.1) {
        COLOR = vec4(bg_col.xyz, alpha);
    } else {
        COLOR = vec4(fg_col.xyz, alpha);
    }
}
