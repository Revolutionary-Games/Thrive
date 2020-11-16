shader_type canvas_item;

uniform vec4 fg_col : hint_color = vec4(0.745098, 0.356863, 0.921569, 1);
uniform vec4 bg_col : hint_color = vec4(0.290196, 0.14902, 0.360784, 1);
void fragment() {
    vec4 screen_col = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0);
    float alpha = texture(TEXTURE, UV).a;
    if ((fg_col - screen_col).x < 0.1) {
        COLOR = vec4(bg_col.xyz, alpha);
    } else {
        COLOR = vec4(fg_col.xyz, alpha);
    }
}