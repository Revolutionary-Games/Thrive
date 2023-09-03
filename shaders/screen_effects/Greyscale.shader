shader_type canvas_item;

const vec3 scaler = vec3(0.2126729f, 0.7151522f, 0.072175f);

void fragment()
{
    vec4 pixelColor = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0);
    
    float avg = dot(pixelColor.rgb, scaler);

    COLOR = vec4(avg, avg, avg, 1);
}
