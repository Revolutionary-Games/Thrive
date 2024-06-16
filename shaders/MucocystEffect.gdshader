shader_type spatial;
render_mode unshaded;

// Any changes to these two must be matched in Membrane.gdshader
// Set to 0 to disable wiggle
uniform float wigglyNess = 1.f;
uniform float movementWigglyNess = 1.f;

uniform float fade = 0.25f;
uniform vec3 tint : source_color = vec3(0.40, 0.80, 0.60);

void vertex(){
    vec3 worldVertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float size = length(VERTEX);

	//worldVertex /= 2.f;

    // Any changes to this must be matched in Membrane.gdshader
    VERTEX.x += sin(worldVertex.z * movementWigglyNess + TIME / 4.0f) / 10.f
        * wigglyNess * size;
    VERTEX.z += sin(worldVertex.x * movementWigglyNess - TIME / 4.0f) / 10.f
        * wigglyNess * size;
	
	//VERTEX.y = 5.f;
}

void fragment() {
    float outer_edge = UV.y - 0.45f;
	float inner_edge = (0.5f - UV.y) - 0.1f;
	
    float mask = max(0.f, outer_edge*10.f);
    float mask2 = max(0.f, inner_edge*(1.f+fade));

    ALBEDO = tint;
    ALPHA = clamp(0.7f - (mask + mask2), 0.f, 1.f);
}
