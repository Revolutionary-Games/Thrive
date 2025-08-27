shader_type spatial;
render_mode depth_prepass_alpha;

// Any changes to these two must be matched in EngulfEffect.gdshader
// Set to 0 to disable wiggle
uniform float wigglyNess = 1.0f;
uniform float movementWigglyNess = 1.0f;
uniform float transparencyTextureFactor = 1.2f;
uniform float turn = 1.0f;

uniform sampler2D albedoTexture : source_color;
uniform sampler2D damagedTexture : source_color;
uniform sampler2D transparencyTexture : source_color;

uniform sampler2D normalTexture;
uniform sampler2D normal2Texture;

uniform sampler2D dissolveTexture : source_color;
uniform float dissolveValue : hint_range(0, 1) = 0.0f;

uniform float healthFraction : hint_range(0, 1) = 0.5f;
uniform vec4 tint : source_color = vec4(1.0f, 1.0f, 1.0f, 1.0f);

void vertex(){
    vec3 worldVertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float size = length(VERTEX);

    // Copy non-distorted vertex for normal map
    UV2 = VERTEX.xz * -0.07f;

    // Any changes to this must be matched in EngulfEffect.gdshader
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


    float verticalAngle = NODE_POSITION_WORLD.z * movementWigglyNess * 0.25f
        + wigglyNess * worldVertex.z * 1.5f + TIME / 4.0f;
    float horizontalAngle = NODE_POSITION_WORLD.x * movementWigglyNess * 0.25f
        + wigglyNess * worldVertex.x * 1.5f + TIME / 4.0f;
    VERTEX.y += (sin(verticalAngle) + sin(horizontalAngle) + 2.0f)
        * 0.20f * wigglyNess * abs(worldVertex.y);

    vec2 verticalDerivative = vec2(cos(verticalAngle), 1.0f);
    vec2 horizontalDerivative = vec2(cos(horizontalAngle), 1.0f);

    vec3 worldspaceNormal = normalize(vec3(-horizontalDerivative.x, (horizontalDerivative.y + verticalDerivative.y) * 0.5f, -verticalDerivative.x))
        * 0.1f * wigglyNess * abs(worldVertex.y);
    vec3 worldspaceTangent = normalize(vec3(horizontalDerivative.y, (-horizontalDerivative.x - verticalDerivative.x) * 0.5f, verticalDerivative.y))
        * 0.1f * wigglyNess * abs(worldVertex.y);

    // Convert worldspace normals into model space and apply,
    // also make wave normals less visible for faster microbes to reduce flickering
    NORMAL = normalize(NORMAL + (inverse(MODEL_MATRIX) * vec4(worldspaceNormal, 0.0f)).xyz
        * (1.5f - movementWigglyNess));
    TANGENT = normalize(TANGENT + (inverse(MODEL_MATRIX) * vec4(worldspaceTangent, 0.0f)).xyz
        * (1.5f - movementWigglyNess));
    BINORMAL = cross(NORMAL, TANGENT);
}

float fresnel(float power, vec3 normal, vec3 view)
{
    return clamp(pow(((1.0f - clamp(dot(normal, view), 0.1f, 1.0f)) * 0.85f + 0.15f) * 3.85f, power), 0.1f, 1.0f);
}

void fragment(){
    vec4 albedo = texture(albedoTexture, UV);
    const float blendAmount = 0.4f;

    vec4 normalmap1 = texture(normalTexture, UV);
    vec4 normalmap2 = texture(normal2Texture, UV2);

    vec3 scaledNormal1 = normalmap1.xyz * 2.0f - 1.0f;
    vec3 scaledNormal2 = normalmap2.xyz * 2.0f - 1.0f;
    vec3 normalMix = vec3((scaledNormal1.xy * (1.0f - blendAmount)) + (scaledNormal2.xy * blendAmount),
        scaledNormal1.z * scaledNormal2.z);
    normalMix = normalize(normalMix);
    normalMix = normalMix * 0.5f + 0.5f;
    NORMAL_MAP = normalMix;
    NORMAL_MAP_DEPTH = 0.5f;

    vec4 damaged = texture(damagedTexture, UV);
    vec4 final = ((albedo * healthFraction) +
        (damaged * (1.0f - healthFraction))) * tint;

    vec4 dissolveTex = texture(dissolveTexture, UV);
    vec4 transparencyTex = texture(transparencyTexture, UV);

    float cutoff = dot(dissolveTex.rgb, vec3(0.34f, 0.34f, 0.34f)) -
        float(-0.482f + clamp(dissolveValue, 0.0f, 1.0f));

    vec3 adjustedView = VIEW;

    // Move fresnel's center closer to membrane's top
    // Not an absolutely necessary step, but it makes fresnel look better
    adjustedView = normalize(adjustedView + vec3(0.0f, 0.0f, 2.0f));

    normalmap2 = normalmap2 * 2.0f - 1.0f;

    vec3 adjustedNormals = normalmap2.z * NORMAL
        + normalmap2.x * TANGENT
        + normalmap2.y * BINORMAL;

    // Mix base normals with detail normals in some proportion (0.0 - 1.0)
    adjustedNormals = normalize(mix(NORMAL, adjustedNormals, 0.2f));

	float textureAlphaModified = albedo.a * albedo.a * albedo.a * 8.0f;

    ALBEDO = final.rgb;
    ALPHA = fresnel(3.0, adjustedNormals, adjustedView) * round(cutoff) * min(transparencyTex.a * (transparencyTextureFactor + textureAlphaModified), 2.0);
	EMISSION = final.rgb * 0.3 * ALPHA;
    ROUGHNESS = 0.7f;
}
