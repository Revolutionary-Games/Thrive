shader_type spatial;

// Set to 0 to disable wiggle
uniform float wigglyNess = 1.f;

uniform float movementWigglyNess = 1.f;

uniform float waviness = 20.f;
uniform float waveSpeed = 10.f;
uniform float waveCutoff = 0.3f;

uniform vec3 tint : source_color = vec3(1, 1, 1);

void vertex(){
    vec3 worldVertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float size = length(VERTEX);
    
    VERTEX.x += sin(worldVertex.z * movementWigglyNess + TIME / 4.0f) / 10.f
        * wigglyNess * size;
    VERTEX.z += sin(worldVertex.x * movementWigglyNess - TIME / 4.0f) / 10.f
        * wigglyNess * size;
}

void fragment() {
    
    float waves = max(0, sin(UV.y * waviness + TIME * waveSpeed));
    vec3 final = vec3(waves) * tint;
    
    ALBEDO = final;
	ALPHA = round(waves - waveCutoff) * (1.f - UV.y);
}
