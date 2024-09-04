shader_type spatial;
render_mode depth_prepass_alpha;

// Any changes to these two must be matched in EngulfEffect.gdshader
// Set to 0 to disable wiggle
uniform float wigglyNess = 1.f;
uniform float movementWigglyNess = 1.f;

uniform sampler2D albedoTexture : source_color;
uniform sampler2D damagedTexture : source_color;

uniform sampler2D normalTexture;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1);

uniform float healthFraction : hint_range(0, 1) = 0.5f;
uniform vec4 tint : source_color = vec4(1, 1, 1, 1);

void vertex(){
    vec3 worldVertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float size = length(VERTEX);
    
    // Any changes to this must be matched in EngulfEffect.gdshader
    VERTEX.x += sin(worldVertex.z * movementWigglyNess + TIME / 4.0f) / 10.f
        * wigglyNess * size;
    VERTEX.z += sin(worldVertex.x * movementWigglyNess - TIME / 4.0f) / 10.f
        * wigglyNess * size;
}

float fresnel(float power, vec3 normal, vec3 view)
{
	return clamp(pow(((1.0 - clamp(dot(normal, view), 0.0, 1.0)) * 0.875 + 0.125) * 3.0, power), 0.0, 1.0);
}

void fragment(){
    vec4 albedo = texture(albedoTexture, UV);

    vec4 normalmap = texture(normalTexture, UV);
	NORMAL_MAP = normalmap.xyz;
	NORMAL_MAP_DEPTH = 1.5f;

    vec4 damaged = texture(damagedTexture, UV);
    vec4 final = ((albedo * healthFraction) +
        (damaged * (1.f - healthFraction))) * tint;

    vec4 dissolveTex = texture(dissolveTexture, UV);

    float cutoff = dot(dissolveTex.rgb, vec3(0.34, 0.34, 0.34)) -
        float(-0.482 + clamp(dissolveValue, 0, 1));

	vec3 adjusted_view = VIEW;
	// Not an absolutely necessary step, but it makes fresnel look better
	adjusted_view = normalize(adjusted_view + vec3(0.0, 0.0, 2.0));

	normalmap = normalmap * 2.0 - 1.0;

	vec3 normals = normalmap.z * NORMAL
	+ normalmap.x * TANGENT
	+ normalmap.y * BINORMAL;

	normals = normalize(mix(NORMAL, normals, 1.0));

	ALBEDO = final.xyz;
    ALPHA = fresnel(3.0, normals, adjusted_view);
    // DEPTH = 0.0f;
	ROUGHNESS = 0.7;
}
