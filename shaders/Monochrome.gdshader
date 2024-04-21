shader_type canvas_item;

const vec3 rgbMultipliers = vec3(0.30, 0.59, 0.11);

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    COLOR.rgba = vec4(vec3(dot(col.xyz, rgbMultipliers)), col.a);
}
