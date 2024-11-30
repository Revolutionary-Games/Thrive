shader_type spatial;
render_mode unshaded, depth_draw_never;

uniform sampler2D textureAlbedo : source_color, filter_linear_mipmap, repeat_enable;

uniform vec2 blurStep = vec2(1.0f, 0.0f);

uniform bool applyBlur = true;

void fragment() {
    if (applyBlur)
    {
        vec2 s = blurStep / vec2(textureSize(textureAlbedo, 0));
        
        // Use SCREEN_UV instead of UV, because otherwise the textureAlbedo is stretched
        // across the plane and only a small part of it is visible on the screen
        ALBEDO =
            0.057433154f * texture(textureAlbedo, SCREEN_UV - 8.0f * s).rgb +
            0.09469126f * texture(textureAlbedo, SCREEN_UV - 6.4f * s).rgb +
            0.13970186f * texture(textureAlbedo, SCREEN_UV - 4.8f * s).rgb +
            0.18443339f * texture(textureAlbedo, SCREEN_UV - 3.2f * s).rgb +
            0.21788229f * texture(textureAlbedo, SCREEN_UV - 1.6f * s).rgb +
            0.23032942f * texture(textureAlbedo, SCREEN_UV - 0.0f * s).rgb +
            0.21788229f * texture(textureAlbedo, SCREEN_UV + 1.6f * s).rgb +
            0.18443339f * texture(textureAlbedo, SCREEN_UV + 3.2f * s).rgb +
            0.13970186f * texture(textureAlbedo, SCREEN_UV + 4.8f * s).rgb +
            0.09469126f * texture(textureAlbedo, SCREEN_UV + 6.4f * s).rgb +
            0.057433154f * texture(textureAlbedo, SCREEN_UV + 8.0f * s).rgb;
    }
    else
    {
        ALBEDO = texture(textureAlbedo, SCREEN_UV).rgb;
    }
}
