shader_type spatial;
render_mode depth_draw_opaque;

uniform sampler2D texture : source_color;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1);


uniform float outlineWidth;
uniform vec4 growColor : source_color;

uniform vec4 tint : source_color = vec4(1, 1, 1, 1);

uniform float jiggleAmount = 0.0005f;

const float jiggleMaxAngle = 15.f;
const float jiggleTimeMultiplier = 0.5f;

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

    float cutoff = dot(dissolveTex.rgb, vec3(0.3, 0.3, 0.3)) -
        float(-0.5 + clamp(dissolveValue, 0, 1));

    vec3 dissolveOutline = vec3(round(1.0 - float(cutoff - outlineWidth))) *
        growColor.rgb;

    ALBEDO = final.rgb;

    EMISSION = dissolveOutline;
}
