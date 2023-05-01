shader_type canvas_item;

const float color_count = 4.0f;
const int pixel_count = 512;

const vec3 palette[4] = {
    vec3(0.0f, 63.0f, 0.0f) / 255.0f,
    vec3(47.0f, 114.0f, 32.0f) / 255.0f,
    vec3(140.0f, 191.0f, 10.0f) / 255.0f,
    vec3(161.0f, 206.0f, 10.0f) / 255.0f
};

void fragment()
{
    vec2 grid_UV = (vec2(ivec2( SCREEN_UV * float(pixel_count) ))+0.5f)/float(pixel_count);
    vec4 pixelColor = textureLod(SCREEN_TEXTURE, grid_UV, 0);
    
    float avg = (pixelColor.r + pixelColor.g + pixelColor.b) / 3.0f;
    int color = int(floor(avg * (color_count - 1.0f) + 0.5f));

    COLOR = vec4(palette[color], 1);
}
