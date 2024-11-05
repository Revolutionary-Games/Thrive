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

    // Copy non-distorted vertex for bump map
    UV2 = VERTEX.xz * 0.05f;

    // Any changes to this must be matched in EngulfEffect.gdshader
    VERTEX.x += sin(worldVertex.z * movementWigglyNess + TIME / 4.0f) / 10.f
        * wigglyNess * size;
    VERTEX.z += sin(worldVertex.x * movementWigglyNess - TIME / 4.0f) / 10.f
        * wigglyNess * size;

    float verticalAngle = NODE_POSITION_WORLD.z * movementWigglyNess * 0.75f
        + movementWigglyNess * worldVertex.z * 1.5f + TIME / 4.0f;
    float horizontalAngle = NODE_POSITION_WORLD.x * movementWigglyNess * 0.75f
        + movementWigglyNess * worldVertex.x * 1.5f + TIME / 4.0f;
    VERTEX.y += (sin(verticalAngle) + sin(horizontalAngle) + 2.0f)
        * 0.35f * wigglyNess * abs(worldVertex.y);

    vec2 verticalDerivative = vec2(cos(verticalAngle), 1.0f);
    vec2 horizontalDerivative = vec2(cos(horizontalAngle), 1.0f);

    vec3 worldspaceNormal = normalize(vec3(-horizontalDerivative.x, (horizontalDerivative.y + verticalDerivative.y) * 0.5f, -verticalDerivative.x))
        * 0.1f * wigglyNess * abs(worldVertex.y);
    vec3 worldspaceTangent = normalize(vec3(horizontalDerivative.y, (-horizontalDerivative.x - verticalDerivative.x) * 0.5f, verticalDerivative.y))
        * 0.1f * wigglyNess * abs(worldVertex.y);

    // Convert worldspace normals into model space and apply
    NORMAL += (inverse(MODEL_MATRIX) * vec4(worldspaceNormal, 0.0f)).xyz;
    TANGENT += (inverse(MODEL_MATRIX) * vec4(worldspaceTangent, 0.0f)).xyz;

    NORMAL = normalize(NORMAL);
    TANGENT = normalize(TANGENT);
    BINORMAL = cross(NORMAL, TANGENT);
}

float fresnel(float power, vec3 normal, vec3 view)
{
    return clamp(pow(((1.0 - clamp(dot(normal, view), 0.0, 1.0)) * 0.85 + 0.15) * 3.85, power), 0.1, 1.0);
}

void fragment(){
    vec4 albedo = texture(albedoTexture, UV);

    vec4 normalmap = texture(normalTexture, UV2);
    NORMAL_MAP = normalmap.xyz;
    NORMAL_MAP_DEPTH = 0.5f;

    vec4 damaged = texture(damagedTexture, UV);
    vec4 final = ((albedo * healthFraction) +
        (damaged * (1.f - healthFraction))) * tint;

    vec4 dissolveTex = texture(dissolveTexture, UV);

    float cutoff = dot(dissolveTex.rgb, vec3(0.34, 0.34, 0.34)) -
        float(-0.482 + clamp(dissolveValue, 0, 1));

    vec3 adjusted_view = VIEW;
    // Move fresnel's center closer to membrane's top
    // Not an absolutely necessary step, but it makes fresnel look better
    adjusted_view = normalize(adjusted_view + vec3(0.0, 0.0, 2.0));

    normalmap = normalmap * 2.0 - 1.0;

    vec3 adjusted_normals = normalmap.z * NORMAL
        + normalmap.x * TANGENT
        + normalmap.y * BINORMAL;

    // Mix base normals with detail normals in some proportion (0.0 - 1.0)
    adjusted_normals = normalize(mix(NORMAL, adjusted_normals, 0.2));

    ALBEDO = final.rgb;
    ALPHA = fresnel(3.0, adjusted_normals, adjusted_view) * round(cutoff);
    ROUGHNESS = 0.7;
}
