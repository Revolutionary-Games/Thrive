shader_type canvas_item;

const vec4 rgbMultipliers = vec4(0.30, 0.59, 0.11, 1);

void fragment() {
    COLOR = texture(TEXTURE, UV) * rgbMultipliers;
    float avg = (COLOR.r + COLOR.g + COLOR.b) / 3.0;
    COLOR.rgb = vec3(avg);
}