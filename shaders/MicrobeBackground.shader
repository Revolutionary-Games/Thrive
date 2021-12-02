shader_type spatial;
render_mode unshaded;

uniform vec2 repeats = vec2(1.0f, 1.0f);
uniform sampler2D layer0 : hint_albedo;
uniform sampler2D layer1 : hint_albedo;
uniform sampler2D layer2 : hint_albedo;
uniform sampler2D layer3 : hint_albedo;

const vec2 speed0 = vec2(3300.0f);
const vec2 speed1 = vec2(2550.0f);
const vec2 speed2 = vec2(1800.0f);
const vec2 speed3 = vec2(1050.0f);

varying vec2 UV3;
varying vec2 UV4;

void vertex(){
    vec2 offset = (repeats - 1.0f) / 2.0f;
    vec2 worldPos = (CAMERA_MATRIX * vec4(0.0f, 0.0f, 0.0f, 1.0f)).xz;

    UV = (UV + worldPos / (speed0 * repeats)) * repeats - offset;
    UV2 = (0.12f + UV + worldPos / (speed1));
    UV3 = (0.512f + UV + worldPos / (speed2));
    UV4 = (0.05f + UV + worldPos / (speed3));
}

void fragment(){
    vec4 colour0 = texture(layer0, UV);
    vec4 colour1 = texture(layer1, UV2);
    vec4 colour2 = texture(layer2, UV3);
    vec4 colour3 = texture(layer3, UV4);

    ALBEDO.rgb =
        colour0.rgb 
        + colour1.rgb * colour1.a * 0.7f
        + colour2.rgb * colour2.a * 0.7f
        + colour3.rgb * colour3.a * 0.7f; 

    ALPHA = 1.0f;
}
