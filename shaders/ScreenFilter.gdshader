shader_type canvas_item;

uniform int mode = 0;
uniform sampler2D screenTexture : hint_screen_texture, filter_linear_mipmap;

void fragment() {
    vec4 px = texture(screenTexture, SCREEN_UV);
    if (mode==1)
    {
        float rgDiff = px.r / px.g;
        float grDiff = px.g / px.r;
        px.r = pow(px.r, 0.7);
        px.g = pow(px.g, 0.7);
        px.rg *= rgDiff > 1.0 ? rgDiff : 1.0;
        px.b *= grDiff > 1.0 ? grDiff : 1.0;
    }
    else if (mode==2)
    {
        float ybDiff = (px.r + px.g) / (px.b * 2.0);
        float byDiff = (2.0 * px.b) / (px.r + px.g);
        px.r *= ybDiff > 1.0 ? ybDiff : 1.0;
        px.g *= byDiff > 1.0 ? byDiff : 1.0;
    }
    else
    {
        // Invalid mode
        // Random crazy filter
        // If this filter is shown, there's a bug
        px = px.rrbb;
    }
    COLOR = vec4(px.rgb,1.0);
}
