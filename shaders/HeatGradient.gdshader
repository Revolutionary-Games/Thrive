shader_type spatial;
render_mode blend_mix, depth_draw_never, depth_test_disabled;
render_mode unshaded;

uniform sampler2D heat;
uniform sampler2D noise;

uniform vec4 warmColour : source_color = vec4(1, 0, 0, 1);
uniform vec4 coldColour : source_color = vec4(0, 0, 1, 1);

uniform vec2 uvOffset = vec2(0, 0);

uniform float heatThreshold : hint_range(0, 1) = 0.5;
uniform float alphaMultiplier : hint_range(0, 1) = 1.0;
uniform float noiseMultiplier : hint_range(0, 1) = 1.0;

void fragment() {
    vec2 scaledUV = UV + uvOffset;
    float heatValue = texture(heat, scaledUV).r;

	// Apply the heat threshold
	float warmth = heatValue - heatThreshold;
	float coldness = 1.0f - warmth - heatThreshold;

    vec4 colour = warmColour * warmth + coldColour * coldness;

	// Calculate extra noise to make the effect more interesting looking
	float noiseValue = texture(noise, scaledUV).r;
	noiseValue = 1.0f + (0.5f - noiseValue * noiseMultiplier);

    ALPHA = alphaMultiplier * (max(heatValue, 1.0f - heatValue) * noiseValue);
    ALBEDO = colour.rgb;
}
