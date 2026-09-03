shader_type spatial;
render_mode unshaded;

uniform sampler2D mainTexture : source_color, repeat_disable;

void vertex() {
    float angle = -TIME * 2.0f;
    mat2 rotationMatrix = mat2(vec2(cos(angle), -sin(angle)),
        vec2(sin(angle), cos(angle)));

    vec2 uv = (UV - vec2(0.5)) * rotationMatrix + vec2(0.5);
    UV = uv;
    
    VERTEX = (vec4(VERTEX, 1.0f) * VIEW_MATRIX).xyz;
}

void fragment(){
    vec4 color = texture(mainTexture, UV);
    ALBEDO = color.rgb;
    ALPHA = color.a;
}
