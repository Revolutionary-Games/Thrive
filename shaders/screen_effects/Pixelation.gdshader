shader_type canvas_item;

uniform int color_count = 32;
uniform int pixel_count = 256;

void fragment()
{
    vec2 grid_UV = (vec2(ivec2( SCREEN_UV * float(pixel_count) ))+0.5)/float(pixel_count);
   
    // This would probably look better if I did this to an hsv, but that's too much for such a tiny effect
    vec4 pixelColor = textureLod(SCREEN_TEXTURE, grid_UV, 0);

    pixelColor = floor(pixelColor * float(color_count - 1) + 0.5) / float(color_count - 1);
        
    COLOR = pixelColor;
}