shader_type canvas_item;

uniform vec4 bg_colour : hint_color;

uniform vec4 main_colour : hint_color;
uniform vec4 sec_colour : hint_color;

void fragment() {
    vec4 screen_colour = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0);
    float alpha = texture(TEXTURE, UV).a;

    if (abs((bg_colour - screen_colour).x) < 0.1) {
        COLOR = vec4(main_colour.xyz, alpha);
    } else {
        COLOR = vec4(sec_colour.xyz, alpha);
    }
}
