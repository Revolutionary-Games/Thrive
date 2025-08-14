shader_type spatial;
render_mode unshaded;

// Any changes to these two must be matched in Membrane.gdshader
// Set to 0 to disable wiggle
uniform float wigglyNess = 1.f;
uniform float movementWigglyNess = 1.f;
uniform float turn = 1.0f;

uniform sampler2D centerMembraneTexture : source_color;
uniform sampler2D albedoTexture : source_color, filter_linear;

void vertex(){
    vec3 worldVertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float size = length(VERTEX);

    // Any changes to this must be matched in Membrane.gdshader
    VERTEX.x += sin(worldVertex.z * movementWigglyNess + TIME / 4.0f) / 10.f
        * wigglyNess * size;
    VERTEX.z += sin(worldVertex.x * movementWigglyNess - TIME / 4.0f) / 10.f
        * wigglyNess * size;

		// Bend when turning
	if (VERTEX.z < 0.2f)
	{
		//VERTEX.z -= turn * 0.75f;
    	VERTEX.x += VERTEX.z * turn * 0.5f;
	}
}

void fragment() {
    vec4 color = texture(albedoTexture, UV);
    vec4 centerTransparencyTex = texture(centerMembraneTexture, UV);

	ALBEDO = color.rgb;
	ALPHA = color.a * 0.6f * (1.0f - centerTransparencyTex.a * 0.25f);
	EMISSION = color.rgb * 3.0f;
}
