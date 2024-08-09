shader_type spatial;
render_mode blend_add,depth_draw_opaque,cull_disabled,diffuse_lambert_wrap,specular_schlick_ggx;

uniform vec4 albedo : source_color = vec4(1.0,1.0,1.0,1.0);
uniform sampler2D texture_albedo : source_color,filter_linear_mipmap,repeat_enable;

uniform float roughness : hint_range(0,1) = 0.3;
uniform sampler2D texture_roughness : hint_roughness_r,filter_linear_mipmap,repeat_enable;
uniform vec4 roughness_texture_channel = vec4(1.0,0.0,0.0,0.0);

uniform float specular : hint_range(0,1) = 0.7;
uniform float metallic : hint_range(0,1) = 0.8;
uniform vec4 metallic_texture_channel = vec4(1.0,0.0,0.0,0.0);

uniform vec4 emission : source_color = vec4(0.1,0.1,0.1,1.0);
uniform float emission_energy = 0.25;

uniform float clearcoat : hint_range(0,1) = 0.3;
uniform float clearcoat_roughness : hint_range(0,1) = 0.1;

//uniform sampler2D normalTexture;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1);
uniform float outlineWidth;
uniform vec4 growColor : source_color;


void fragment() {
    vec2 base_uv = UV;
    vec4 albedo_tex = texture(texture_albedo,base_uv);
    albedo_tex.rgb = mix(pow((albedo_tex.rgb + vec3(0.055)) * (1.0 / (1.0 + 0.055)),vec3(2.4)),albedo_tex.rgb.rgb * (1.0 / 12.92),lessThan(albedo_tex.rgb,vec3(0.04045)));
        
    vec4 dissolveTex = texture(dissolveTexture, UV);
    float cutoff = dot(dissolveTex.rgb, vec3(0.382, 0.382, 0.382)) -
        float(-0.487 + clamp(dissolveValue, 0, 1));
    vec3 dissolveOutline = vec3(round(1.0 - float(cutoff - outlineWidth))) *
        growColor.rgb;

    ALBEDO = albedo.rgb * albedo_tex.rgb;
    float roughness_tex = 1.f - dot(texture(texture_roughness,base_uv),roughness_texture_channel);
    ROUGHNESS = roughness_tex * roughness;
    float metallic_tex = dot(texture(texture_roughness,base_uv),metallic_texture_channel);
    METALLIC = metallic_tex * metallic;
    SPECULAR = specular;
    float emission_tex = 1.f - (texture(texture_roughness,base_uv).a);
    ALPHA *= albedo.a * albedo_tex.a;
    if (dissolveValue > 0.f){
        EMISSION = dissolveOutline;
    }else{
        EMISSION = (emission.rgb*emission_tex)*emission_energy;
    }
    CLEARCOAT = clearcoat;	CLEARCOAT_ROUGHNESS = clearcoat_roughness;
}
