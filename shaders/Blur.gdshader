shader_type canvas_item;
render_mode unshaded;

uniform sampler2D textureAlbedo : source_color, filter_linear_mipmap, repeat_enable;

uniform vec2 blurStep = vec2(1.0f, 0.0f);

uniform bool applyBlur = true;

void fragment() {
    if (applyBlur)
    {
        vec2 s = blurStep / vec2(textureSize(textureAlbedo, 0));
        COLOR.rgb =
            0.0016858046f * texture(textureAlbedo, UV - 6.4f * s).rgb +
            0.015835322f * texture(textureAlbedo, UV - 4.8f * s).rgb +
            0.07843286f * texture(textureAlbedo, UV - 3.2f * s).rgb +
            0.20484284f * texture(textureAlbedo, UV - 1.6f * s).rgb +
            0.28209478f * texture(textureAlbedo, UV).rgb +
            0.20484288f * texture(textureAlbedo, UV + 1.6f * s).rgb +
            0.07843288f * texture(textureAlbedo, UV + 3.2f * s).rgb +
            0.01583533f * texture(textureAlbedo, UV + 4.8f * s).rgb +
            0.0016858061f * texture(textureAlbedo, UV + 6.4f * s).rgb;
    }
    else
    {
        COLOR.rgb = texture(textureAlbedo, UV).rgb;
    }
    
    COLOR.a = 1.0f;
}