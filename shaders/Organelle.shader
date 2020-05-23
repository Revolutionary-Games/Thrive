shader_type spatial;

uniform sampler2D texture : hint_albedo;

uniform sampler2D dissolveTexture : hint_albedo;
uniform float dissolveValue : hint_range(0, 1);

uniform vec4 tint : hint_color = vec4(1, 1, 1, 1);

const float jiggleAmount = 0.0005f;
const float jiggleMaxAngle = 15.f;
const float jiggleTimeMultiplier = 0.5f;

const float PI = 3.14159265358979323846;

void vertex(){
    // Offset animation
    VERTEX.x += sin(TIME * 3.0f * jiggleTimeMultiplier) * jiggleAmount;
    VERTEX.y += sin(TIME * 2.0f * jiggleTimeMultiplier) * jiggleAmount;
    
    // Rotation animation
    float angle = cos(TIME * jiggleTimeMultiplier) * PI * jiggleMaxAngle / 360.f;
    mat4 rotation = mat4(
        vec4(cos(angle), -sin(angle), 0.f, 0.f),
        vec4(sin(angle),  cos(angle), 0.f, 0.f),
        vec4(0.f, 0.f, 1.f, 0.f),
        vec4(0.f, 0.f, 0.f, 1.f)
    );
    
    VERTEX = (rotation * vec4(VERTEX, 1.0f)).xyz;
}

void fragment(){
    vec4 normal = texture(texture, UV);
    vec4 final = normal * tint;

	vec4 dissolveTex = texture(dissolveTexture, UV);

	float alpha = dot(dissolveTex.rgb, vec3(0.3, 0.3, 0.3)) -
		float(-0.8 + dissolveValue);

    ALBEDO = final.rgb;
    ALPHA = round(alpha) * final.a;
}
