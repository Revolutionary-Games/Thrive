shader_type spatial;
render_mode unshaded, cull_disabled;

uniform sampler2D mainTexture;
uniform sampler2D projected : repeat_disable;
uniform sampler2D dirt;

uniform mat4[500] projectionMatrices;
uniform int projectionMatrixCount;

uniform sampler2D blendCurve;

uniform vec3 mainTextureScale;

uniform vec3 projectionScale;

varying vec3 pos;
varying vec3 normal;

void vertex() {
    // Called for every vertex the material is visible on.
    normal = NORMAL;
    pos = VERTEX;
    
    // This turns the model into a flat 2D representation of itself.
    // One that coincides with the UV mapping, so that the shader can become a
    // texture when photographed
    VERTEX = vec3(vec2(UV.x, UV.y) - 0.5f, 0.0f);
}

vec4 triplanarProjection(vec3 projNormal, vec3 projPos, sampler2D topTexture,
    sampler2D sideTexture, sampler2D frontTexture)
{
    vec4 top = texture(topTexture, vec2(projPos.x, projPos.z));
    vec4 side = texture(sideTexture, vec2(projPos.z, projPos.y));
    vec4 front = texture(frontTexture, vec2(projPos.x, projPos.y));
    
    projNormal *= projNormal;
    projNormal /= projNormal.x + projNormal.y + projNormal.z;
    
    vec4 result = top * projNormal.y + side * projNormal.x
        + front * projNormal.z;
    
    return result;
}

void fragment() {
    // Called for every pixel the material is visible on.
    ALBEDO = vec3(0.0f);
    
    vec4 skinMix = triplanarProjection(normal, pos * mainTextureScale,
        mainTexture, mainTexture, mainTexture);
    
    vec4 dirtMix = triplanarProjection(normal, pos * mainTextureScale, dirt,
        dirt, dirt);
    
    ALBEDO = skinMix.rgb * dirtMix.rgb * COLOR.rgb;
    
    for (int i = 0; i < projectionMatrixCount; i++)
    {
        mat4 matrix = projectionMatrices[i];
        
        vec3 posProjected = (matrix * vec4(pos, 1.0f)).xyz * projectionScale;
        
        // Set position to zero to make the projected normal unaffected by it
        matrix[3] = vec4(0.0f, 0.0f, 0.0f, 1.0f);
        
        vec3 normalProjected = (matrix * vec4(normal, 1.0f)).xyz;
        
        if (normalProjected.z < 0.2f)
            continue;
        
        if (posProjected.z > 1.0f || posProjected.z < -0.8f
            || max(abs(posProjected.x), abs(posProjected.y)) > 0.5f)
            continue;
        
        vec4 color = texture(projected, posProjected.xy + 0.5f);
        
        ALBEDO = mix(ALBEDO, color.rgb * dirtMix.rgb * COLOR.rgb, color.a);
    }
}
