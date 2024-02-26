shader_type spatial;
render_mode depth_prepass_alpha;

// Set to 0 to disable wiggle
uniform float wigglyNess = 1.f;

uniform float movementWigglyNess = 1.f;

uniform sampler2D albedoTexture : source_color;
uniform sampler2D damagedTexture : source_color;

uniform sampler2D normalTexture;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1);

uniform float healthFraction = 0.5f;
uniform vec4 tint : source_color = vec4(1, 1, 1, 1);


void vertex(){
    vec3 worldVertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float size = length(VERTEX);
    
    VERTEX.x += sin(worldVertex.z * movementWigglyNess + TIME / 4.0f) / 10.f
        * wigglyNess * size;
    VERTEX.z += sin(worldVertex.x * movementWigglyNess - TIME / 4.0f) / 10.f
        * wigglyNess * size;
}

void fragment(){
    vec4 albedo = texture(albedoTexture, UV);
    
    vec4 normalmap = texture(normalTexture, UV);
    vec3 normals = vec3(normalmap.r * 2.0 - 1.0, normalmap.g * 2.0 - 1.0, 
        normalmap.b * 2.0 - 1.0);

    float y = normals.y;
    normals.y = -normals.z;
    normals.z = y;

    vec4 damaged = texture(damagedTexture, UV);
    vec4 final = ((albedo * healthFraction) + 
        (damaged * (1.f - healthFraction))) * tint;

    vec4 dissolveTex = texture(dissolveTexture, UV);

    float cutoff = dot(dissolveTex.rgb, vec3(0.6, 0.6, 0.6)) -
        float(-0.15 + clamp(dissolveValue, 0, 1));

    ALBEDO = final.rgb;
    NORMAL = (INV_VIEW_MATRIX * (MODEL_MATRIX * vec4(normals, 0.0))).xyz;
    ALPHA = round(cutoff) * final.a;
    DEPTH = 0.0f;
}
