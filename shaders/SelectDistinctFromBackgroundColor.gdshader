shader_type canvas_item;

uniform vec4 backgroundColour : source_color;

uniform vec4 mainColour : source_color;
uniform vec4 secondaryColour : source_color;

uniform sampler2D screenTexture : hint_screen_texture, filter_linear_mipmap;

void fragment() {
    vec4 screenColour = textureLod(screenTexture, SCREEN_UV, 0.0);
    float alpha = texture(TEXTURE, UV).a;

    if (abs((backgroundColour - screenColour).x) < 0.1) {
        COLOR = vec4(mainColour.xyz, alpha);
    } else {
        COLOR = vec4(secondaryColour.xyz, alpha);
    }
}
