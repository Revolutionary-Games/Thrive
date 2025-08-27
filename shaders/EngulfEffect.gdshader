shader_type spatial;

// Setting this unshaded makes this very visible (but more glitchy at small sizes)
// render_mode unshaded;

// Any changes to these two must be matched in Membrane.gdshader
// Set to 0 to disable wiggle
uniform float wigglyNess = 1.f;
uniform float movementWigglyNess = 1.f;
uniform float turn = 1.0f;

uniform float waviness = 20.f;
uniform float waveSpeed = 15.f;

uniform float fade : hint_range(0, 1) = 0.0f;

uniform vec3 tint : source_color = vec3(0, 0.55, 0.80);

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
    float waves = max(0, sin(UV.y * waviness + TIME * waveSpeed));
    vec3 final = vec3(waves) * tint;

    ALBEDO = final;
    ALPHA = waves * (1.f - UV.y) * fade;

	// Increase minimum visibility to help in certain backgrounds to be visible
	EMISSION = final * 0.18f;
}
