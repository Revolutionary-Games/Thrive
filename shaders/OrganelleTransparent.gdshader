shader_type spatial;
render_mode depth_prepass_alpha, diffuse_lambert_wrap;

uniform sampler2D texture : source_color;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1) = 0.0;
uniform float dissolveScale : hint_range(0.5, 5.0, 0.1) = 1.0;

uniform float fresnelValue : hint_range(0, 1) = 1.0;
uniform bool fresnelActive = false;

uniform float outlineWidth = 0.0;
uniform vec4 growColor : source_color = vec4(0.5, 0.5, 0.5, 1);

uniform vec4 tint : source_color = vec4(1, 1, 1, 1);

uniform float jiggleAmount = 0.0005f;

const float jiggleMaxAngle = 15.f;
const float jiggleTimeMultiplier = 0.5f;

void vertex(){
    // Offset animation
    VERTEX.x += sin(TIME * 3.0f * jiggleTimeMultiplier) * jiggleAmount;
    VERTEX.z += sin(TIME * 2.0f * jiggleTimeMultiplier) * jiggleAmount;

    // Rotation animation
    float angle = cos(TIME * jiggleTimeMultiplier) * PI * jiggleMaxAngle / 360.f;
    mat4 rotation = mat4(
        vec4(cos(angle), 0.f, -sin(angle), 0.f),
        vec4(0.f, 1.f, 0.f, 0.f),
        vec4(sin(angle),  0.f, cos(angle), 0.f),
        vec4(0.f, 0.f, 0.f, 1.f)
    );

    VERTEX = (rotation * vec4(VERTEX, 1.0f)).xyz;
}

void fragment(){
    vec4 normal = texture(texture, UV);
    vec4 final = normal * tint;

    vec4 dissolveTex = texture(dissolveTexture, (UV * dissolveScale));

    float cutoff = dot(dissolveTex.rgb, vec3(0.3, 0.3, 0.3)) -
        float(-0.5 + clamp(dissolveValue, 0, 1));

    vec3 dissolveOutline = vec3(round(1.0 - float(cutoff - outlineWidth))) *
        growColor.rgb;

    ALBEDO = final.rgb;

    if (fresnelActive)
    {
		float fresnel = pow(dot(NORMAL, VIEW), 1.2) * fresnelValue;
        ALPHA = clamp(round(cutoff) * final.a * (1.05f - fresnel) * 1.5, 0, 1);
    }
    else
    {
        ALPHA = round(cutoff) * final.a;
    }

    EMISSION = clamp(dissolveOutline, 0, dissolveValue);
}
