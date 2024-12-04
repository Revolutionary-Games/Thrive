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
            0.0016858046f * texture(textureAlbedo, SCREEN_UV - 6.4f * s).rgb +
            0.015835322f * texture(textureAlbedo, SCREEN_UV - 4.8f * s).rgb +
            0.07843286f * texture(textureAlbedo, SCREEN_UV - 3.2f * s).rgb +
            0.20484284f * texture(textureAlbedo, SCREEN_UV - 1.6f * s).rgb +
            0.28209478f * texture(textureAlbedo, SCREEN_UV).rgb +
            0.20484288f * texture(textureAlbedo, SCREEN_UV + 1.6f * s).rgb +
            0.07843288f * texture(textureAlbedo, SCREEN_UV + 3.2f * s).rgb +
            0.01583533f * texture(textureAlbedo, SCREEN_UV + 4.8f * s).rgb +
            0.0016858061f * texture(textureAlbedo, SCREEN_UV + 6.4f * s).rgb;
    }
    else
    {
        ALBEDO = texture(textureAlbedo, SCREEN_UV).rgb;
    }
}
