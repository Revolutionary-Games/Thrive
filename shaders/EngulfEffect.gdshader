shader_type spatial;

uniform float waviness = 20.f;
uniform float waveSpeed = 10.f;
uniform float waveCutoff = 0.3f;

uniform vec3 tint : source_color = vec3(1, 1, 1);

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
    
    float waves = max(0, sin(UV.y * waviness + TIME * waveSpeed));
    vec3 final = vec3(waves) * tint;
    
    ALBEDO = final;
	ALPHA = round(waves - waveCutoff) * (1.f - UV.y);
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
