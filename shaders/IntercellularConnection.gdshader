shader_type spatial;

uniform sampler2D mainTexture;
uniform sampler2D normalTexture;

uniform vec3 tint = vec3(1.0f);

void fragment() {
    NORMAL_MAP = texture(normalTexture, UV).rgb;
    
    vec4 color = texture(mainTexture, UV);
    
    ALBEDO = color.rgb * tint;
    ALPHA = color.a;
    SPECULAR = 1.0f;
}
