shader_type spatial;

// Set to 0 to disable wiggle
uniform float wigglyNess = 1.f;

uniform sampler2D albedoTexture;
uniform sampler2D damagedTexture;

uniform float healthFraction = 0.5f;
uniform vec4 tint = vec4(1, 1, 1, 1);

void vertex(){
    vec3 worldVertex = (WORLD_MATRIX * vec4(VERTEX, 1.0)).xyz;
    
    VERTEX.x += (sin(worldVertex.z * 2.f + sign(worldVertex.x) * TIME) / 10.f) 
        * wigglyNess;
    VERTEX.z += (sin(worldVertex.x * 2.f - sign(worldVertex.z) * TIME) / 10.f) 
        * wigglyNess;
}

void fragment(){
    vec4 normal = texture(albedoTexture, UV);
    vec4 damaged = texture(damagedTexture, UV);
    vec4 final = ((normal * healthFraction) + 
        (damaged * (1.f - healthFraction))) * tint;
    ALBEDO = final.rgb;
    ALPHA = final.a;
}