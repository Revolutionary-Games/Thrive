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
            0.057433154f * texture(textureAlbedo, UV - 8.0f * s).rgb +
            0.09469126f * texture(textureAlbedo, UV - 6.4f * s).rgb +
            0.13970186f * texture(textureAlbedo, UV - 4.8f * s).rgb +
            0.18443339f * texture(textureAlbedo, UV - 3.2f * s).rgb +
            0.21788229f * texture(textureAlbedo, UV - 1.6f * s).rgb +
            0.23032942f * texture(textureAlbedo, UV - 0.0f * s).rgb +
            0.21788229f * texture(textureAlbedo, UV + 1.6f * s).rgb +
            0.18443339f * texture(textureAlbedo, UV + 3.2f * s).rgb +
            0.13970186f * texture(textureAlbedo, UV + 4.8f * s).rgb +
            0.09469126f * texture(textureAlbedo, UV + 6.4f * s).rgb +
            0.057433154f * texture(textureAlbedo, UV + 8.0f * s).rgb;
    }
    else
    {
        COLOR.rgb = texture(textureAlbedo, UV).rgb;
    }
    
    COLOR.a = 1.0f;
}