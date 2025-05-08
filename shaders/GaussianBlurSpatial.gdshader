shader_type spatial;
render_mode unshaded, depth_draw_never;

uniform sampler2D textureAlbedo : source_color, filter_linear_mipmap, repeat_enable;

uniform vec2 blurStep = vec2(1.0f, 0.0f);

uniform float blurAmount = 1.0f;

void fragment() {
    vec2 s = blurAmount * blurStep / vec2(textureSize(textureAlbedo, 0));
        
    // Use SCREEN_UV instead of UV, because otherwise the textureAlbedo is stretched
    // across the plane and only a small part of it is visible on the screen
    ALBEDO =
        0.0021587821843544993 * texture(textureAlbedo, SCREEN_UV - 6.4f * s).rgb +
        0.017919574546480063 * texture(textureAlbedo, SCREEN_UV - 4.8f * s).rgb +
        0.08875623000679331 * texture(textureAlbedo, SCREEN_UV - 3.2f * s).rgb +
        0.23180435116461087 * texture(textureAlbedo, SCREEN_UV - 1.6f * s).rgb +
        0.3192242279243133 * texture(textureAlbedo, SCREEN_UV).rgb +
        0.23180439642942974 * texture(textureAlbedo, SCREEN_UV + 1.6f * s).rgb +
        0.08875625263920274 * texture(textureAlbedo, SCREEN_UV + 3.2f * s).rgb +
        0.017919583599443834 * texture(textureAlbedo, SCREEN_UV + 4.8f * s).rgb +
        0.0019076926935783702 * texture(textureAlbedo, SCREEN_UV + 6.4f * s).rgb;
}
