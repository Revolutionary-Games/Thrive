shader_type spatial;
render_mode unshaded;

uniform float xrepeats = 1f;
uniform float yrepeats = 1f;
uniform sampler2D layer0 : hint_albedo;
uniform sampler2D layer1 : hint_albedo;
uniform sampler2D layer2 : hint_albedo;
uniform sampler2D layer3 : hint_albedo;

const float speed0 = 3300.0f;
const float speed01 = 6600.0f;
const float speed1 = 2550.0f;
const float speed11 = 5100.0f;
const float speed2 = 1800.0f;
const float speed21 = 3200.0f;
const float speed3 = 1050.0f;
const float speed31 = 2100.0f;

varying vec2 UV3;
varying vec2 UV4;

void vertex(){
    vec3 worldPos = (CAMERA_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    UV.x = (UV.x + worldPos.x / speed0) * 1.0f;
    UV.y = (UV.y + worldPos.z / speed01) * 1.0f;
    UV2.x = (0.12f + UV.x + worldPos.x / speed1) * 1.0f;
    UV2.y = (0.12f + UV.y + worldPos.z / speed11) * 1.0f;
    UV3.x = (0.512f + UV.x + worldPos.x / speed2) * 1.0f;
    UV3.y = (0.512f + UV.y + worldPos.z / speed21) * 1.0f;
    UV4.x = (0.05f + UV.x + worldPos.x / speed3) * 1.0f;
    UV4.y = (0.05f + UV.y + worldPos.z / speed31) * 1.0f;
}

void fragment(){
    vec2 repeat = vec2(xrepeats, yrepeats);
    vec2 offset = (repeat - 1f) / 2f;

    vec4 colour0 = texture(layer0, UV * repeat - offset);
    vec4 colour1 = texture(layer1, UV2 * repeat - offset);
    vec4 colour2 = texture(layer2, UV3 * repeat - offset);
    vec4 colour3 = texture(layer3, UV4 * repeat - offset);

    ALBEDO.rgb =
        colour0.rgb 
        + colour1.rgb * colour1.a * 0.7f
        + colour2.rgb * colour2.a * 0.7f
        + colour3.rgb * colour3.a * 0.7f; 

    ALPHA = 1.0f;
}
