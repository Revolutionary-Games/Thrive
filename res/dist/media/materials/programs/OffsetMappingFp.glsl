uniform vec4 lightDiffuse;
uniform vec4 scaleBias;
uniform vec4 spotParams;
uniform vec4 lightDiffuse1;
uniform vec4 spotParams1;

uniform sampler2D normalHeightMap;
uniform sampler2D diffuseMap;
uniform sampler2D shadowMap1;
uniform sampler2D shadowMap2;



varying vec3 tangentEyeDir;
varying vec3 tangentLightDir[2];
varying vec3 tangentSpotDir[2];
varying vec4 shadowUV[2];


// Expand a range-compressed vector
vec3 expand(vec3 v)
{
	return (v - 0.5) * 2.0;
}


void main()
{
	// get the height using the tex coords
	float height = texture2D(normalHeightMap, gl_TexCoord[0].xy).a;
	// scale and bias factors	
	float scale = scaleBias.x;
	float bias = scaleBias.y;

	// calculate displacement	
	float displacement = (height * scale) + bias;
	//float displacement = (height * 0.04) - 0.02;
	
	vec3 scaledEyeDir = tangentEyeDir * displacement;
	
	// calculate the new tex coord to use for normal and diffuse
	vec2 newTexCoord = (scaledEyeDir + gl_TexCoord[0].xyz).xy;
	
	// get the new normal and diffuse values
	vec3 normal = expand(texture2D(normalHeightMap, newTexCoord).xyz);
	vec4 diffuse = texture2D(diffuseMap, newTexCoord);
	
	vec4 col1 = diffuse * clamp(dot(normal, tangentLightDir[0]),0.0,1.0) * lightDiffuse;
	// factor in spotlight angle
	float rho = clamp(dot(tangentSpotDir[0], tangentLightDir[0]),0.0,1.0);
	// factor = (rho - cos(outer/2) / cos(inner/2) - cos(outer/2)) ^ falloff
	float spotFactor = pow(
		clamp(rho - spotParams.y,0.0,1.0) / (spotParams.x - spotParams.y), spotParams.z);
	col1 = col1 * spotFactor;
	vec4 col2 = diffuse * clamp(dot(normal, tangentLightDir[1]),0.0,1.0) * lightDiffuse1;
	// factor in spotlight angle
	rho = clamp(dot(tangentSpotDir[1], tangentLightDir[1]),0.0,1.0);
	// factor = (rho - cos(outer/2) / cos(inner/2) - cos(outer/2)) ^ falloff
	spotFactor = pow(
		clamp(rho - spotParams1.y,0.0,1.0) / (spotParams1.x - spotParams1.y), spotParams1.z);
	col2 = col2 * spotFactor;

	// shadow textures
	col1 = col1 * texture2DProj(shadowMap1, shadowUV[0]);
	col2 = col2 * texture2DProj(shadowMap2, shadowUV[1]);

	gl_FragColor = col1 + col2;

}
