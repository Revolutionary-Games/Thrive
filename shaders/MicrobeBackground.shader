shader_type spatial;
render_mode unshaded;

uniform sampler2D layer0;
uniform sampler2D layer1;
uniform sampler2D layer2;
uniform sampler2D layer3;

uniform vec3 cameraPos = vec3(0, 0, 0);

const float speed0 = 1100.0f;
const float speed01 = 2200.0f;
const float speed1 = 850.0f;
const float speed11 = 1700.0f;
const float speed2 = 600.0f;
const float speed21 = 1200.0f;
const float speed3 = 350.0f;
const float speed31 = 700.0f;

varying vec2 UV3;
varying vec2 UV4;

void vertex(){
    UV.x = (UV.x + cameraPos.x / speed0) * 1.0f;
    UV.y = (UV.y + cameraPos.z / speed01) * 1.0f;
    UV2.x = (0.12f + UV.x + cameraPos.x / speed1) * 1.0f;
    UV2.y = (0.12f + UV.y + cameraPos.z / speed11) * 1.0f;
    UV3.x = (0.512f + UV.x + cameraPos.x / speed2) * 1.0f;
    UV3.y = (0.512f + UV.y + cameraPos.z / speed21) * 1.0f;
    UV4.x = (0.05f + UV.x + cameraPos.x / speed3) * 1.0f;
    UV4.y = (0.05f + UV.y + cameraPos.z / speed31) * 1.0f;
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
