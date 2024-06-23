shader_type canvas_item;

uniform vec4 damageColor : source_color = vec4(0.85f, 0, 0, 0.65f);

// Use to fade effect in or out
uniform float fade : hint_range(0, 1) = 0;

// Increase to extend color further towards screen center
uniform float extent : hint_range(-1, 1) = 0.15;

void fragment() {
    float center = 1.f - length(UV - vec2(0.5f, 0.5f));
    float cutout  = damageColor.a - center + extent;
    float alpha = clamp(cutout * fade, 0, damageColor.a);
    COLOR = vec4(damageColor.rgb, (damageColor.a * alpha));
}

