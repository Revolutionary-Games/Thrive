shader_type spatial;
render_mode unshaded;

// Any changes to these two must be matched in Membrane.gdshader
// Set to 0 to disable wiggle
uniform float wigglyNess = 1.f;
uniform float movementWigglyNess = 1.f;

uniform sampler2D albedoTexture : source_color;

void vertex(){
    vec3 worldVertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float size = length(VERTEX);

    // Any changes to this must be matched in Membrane.gdshader
    VERTEX.x += sin(worldVertex.z * movementWigglyNess + TIME / 4.0f) / 10.f
        * wigglyNess * size;
    VERTEX.z += sin(worldVertex.x * movementWigglyNess - TIME / 4.0f) / 10.f
        * wigglyNess * size;
}

void fragment() {
    vec4 color = texture(albedoTexture, UV);

	ALBEDO = color.rgb;
	ALPHA = color.a * 0.15f;
	EMISSION = color.rgb * 2.0;
}
