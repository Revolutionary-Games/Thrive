shader_type canvas_item;

uniform vec4 backgroundColour : hint_color;

uniform vec4 mainColour : hint_color;
uniform vec4 secondaryColour : hint_color;

void fragment() {
    vec4 screenColour = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0);
    float alpha = texture(TEXTURE, UV).a;

    if (abs((backgroundColour - screenColour).x) < 0.1) {
        COLOR = vec4(mainColour.xyz, alpha);
    } else {
        COLOR = vec4(secondaryColour.xyz, alpha);
    }
}
