shader_type spatial;

// With cavity cells the connectors are no longer horizontal and can be seen from the back
render_mode cull_disabled;

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
