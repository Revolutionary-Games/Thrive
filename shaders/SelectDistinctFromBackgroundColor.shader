shader_type canvas_item;

uniform vec4 bg_col : hint_color;

uniform vec4 main_col : hint_color;
uniform vec4 sec_col : hint_color;

void fragment() {
    vec4 screen_col = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0);
    float alpha = texture(TEXTURE, UV).a;

    if (abs((bg_col - screen_col).x) < 0.1) {
        COLOR = vec4(main_col.xyz, alpha);
    } else {
        COLOR = vec4(sec_col.xyz, alpha);
    }
}
