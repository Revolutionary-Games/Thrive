// Intended for light-emitting vacuole

shader_type spatial;
render_mode depth_prepass_alpha, blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

uniform sampler2D albedoTexture : source_color, filter_linear_mipmap, repeat_enable;

uniform float dissolveValue : hint_range(0, 1);
uniform float dissolveScale = 1.0;

uniform vec4 tint : source_color = vec4(1, 1, 1, 1);

uniform float roughness : hint_range(0.0, 1.0);
uniform sampler2D roughnessTexture : hint_roughness_r, filter_linear_mipmap, repeat_enable;

uniform float specular : hint_range(0.0, 1.0, 0.01);

uniform sampler2D emissionTexture : source_color, hint_default_black, filter_linear_mipmap, repeat_enable;
uniform vec4 emission : source_color = vec4(0.08, 0.08, 0.08, 1.0);
uniform float emissionEnergy : hint_range(0.0, 100.0, 0.01) = 1;

uniform float outlineWidth;
uniform vec4 growColor : source_color;

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
    vec2 base_uv = UV;
    vec4 final = texture(albedoTexture, base_uv) * tint;

    vec4 dissolveTex = texture(dissolveTexture, (base_uv * dissolveScale));

    float cutoff = dot(dissolveTex.rgb, vec3(0.3, 0.3, 0.3)) -
        float(-0.5 + clamp(dissolveValue, 0, 1));

    vec3 dissolveOutline = vec3(round(1.0 - float(cutoff - outlineWidth))) *
        growColor.rgb;

    ALBEDO = final.rgb;

    SPECULAR = specular;

    vec4 roughness_texture_channel = vec4(1.0, 0.0, 0.0, 0.0);
    float roughness_tex = dot(texture(roughnessTexture, base_uv), roughness_texture_channel);
    ROUGHNESS = roughness_tex * roughness;

    vec3 emission_tex = texture(emissionTexture, base_uv).rgb;

    // Emission Operator: Add
    EMISSION = clamp((dissolveOutline + emission.rgb + emission_tex) * emissionEnergy, 0, emissionEnergy);

    ALPHA = round(cutoff) * final.a;
}
