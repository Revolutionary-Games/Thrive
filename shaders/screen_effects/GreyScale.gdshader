shader_type canvas_item;

void fragment()
{
    vec4 pixelColor = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0);
    
    // I know there are nicer looking ways to do greyscale, but this will do for now
    float avg = (pixelColor.r + pixelColor.g + pixelColor.b) / 3.0;

    COLOR = vec4(avg, avg, avg, 1);
}