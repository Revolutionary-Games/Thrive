// Intended for thermoplast and light-emitting vacuole. Uses EMISSION to light up the mesh.
// Values above 1.0 for emissionEnergy cause a glow around the organelle (if Bloom is active).
// Use emissionTexture to have only part of the mesh glow, or ignore it to affect the whole mesh.
shader_type spatial;
render_mode depth_prepass_alpha, blend_mix, depth_draw_opaque, cull_back;

uniform sampler2D albedoTexture : source_color, filter_linear_mipmap, repeat_enable;

uniform float dissolveValue : hint_range(0, 1) = 0.f;
uniform float dissolveScale : hint_range(0.5, 5.0, 0.1) = 1.f;

uniform vec4 tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);

uniform float roughness : hint_range(0.0, 1.0) = 0.5f;
uniform float specular : hint_range(0.0, 1.0, 0.01) = 0.2f;

uniform sampler2D emissionTexture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 emission : source_color = vec4(0.08f, 0.08f, 0.08f, 1.f);
uniform float emissionEnergy : hint_range(0.0, 50.0, 0.01) = 1.f;
uniform bool useTextureEmission = false;

uniform float outlineWidth : hint_range(0.0, 0.25, 0.01) = 0.0;
uniform vec4 growColor : source_color = vec4(0.1, 0.1, 0.1, 1.0);

uniform float jiggleAmount = 0.0005f;

const float jiggleMaxAngle = 15.f;
const float jiggleTimeMultiplier = 0.5f;

uniform sampler2D dissolveTexture : source_color;

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

void fragment() {
    vec4 final = texture(albedoTexture, UV) * tint;

    vec4 dissolveTex = texture(dissolveTexture, (UV * dissolveScale));

    float cutoff = dot(dissolveTex.rgb, vec3(0.3f, 0.3f, 0.3f)) -
        float(-0.5f + dissolveValue);

    vec3 dissolveOutline = clamp(vec3(round(1.f - float(cutoff - outlineWidth))) *
        growColor.rgb, 0.f, dissolveValue);

    ALBEDO = final.rgb;

    SPECULAR = specular;

    ROUGHNESS = roughness;

    vec3 pixelEmission = vec3(1.f, 1.f, 1.f);
    if (useTextureEmission)
    {
        vec3 emissionTex = texture(emissionTexture, UV).rgb;
        pixelEmission = emissionTex * 10.f;
    }

    EMISSION = dissolveOutline + (emission.rgb * pixelEmission * emissionEnergy);

    ALPHA = round(cutoff) * final.a;
}
