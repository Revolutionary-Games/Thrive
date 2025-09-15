shader_type sky;

uniform vec3 colour : source_color;

void sky() {
    COLOR = colour;
}
