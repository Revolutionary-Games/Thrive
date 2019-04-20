#version 330

uniform mat4 worldViewProj;
uniform float time;
in vec3 normal;
uniform vec4 lightSpecular, lightDiffuse, lightDirection, lightPosition;
in vec4 vertex;
in vec4 uv0;

out vec2 UV;
out vec4 vp_color;

void main()
{
    vec4 position = vertex;
    position.z = vertex.z;
    position.x = vertex.x;
    gl_Position = worldViewProj * position;
	
	// vertex normal in world space
	vec3 _normal = normalize(normal);
	
    // directional light direction
	vec3 _lightDirection = normalize(lightPosition.xyz);
	
	//ndot
	float _NdotL = max(dot(_normal, _lightDirection), 0.0);
	
	// compute diffuse term
	vec4 _diffuse =  _NdotL * lightDiffuse;	
	
	// compute specular term
	vec4 _specular = lightSpecular;
		
	
	// resulting color (e.g. the light) will be interpolated over the triangle
	vp_color = vec4((_diffuse + _specular).xyz, 1.0);
	
    UV = uv0.xy;
}
