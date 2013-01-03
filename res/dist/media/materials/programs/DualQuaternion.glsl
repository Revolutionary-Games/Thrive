#version 120

mat2x4 blendTwoWeightsAntipod(vec4 blendWgt, vec4 blendIdx, vec4 dualQuaternions[24]);
vec3 calculateBlendPosition(vec3 position, mat2x4 blendDQ);
vec3 calculateBlendNormal(vec3 normal, mat2x4 blendDQ);

uniform vec4 worldDualQuaternion2x4Array[24];
uniform mat4 viewProjectionMatrix;
uniform vec4   lightPos[2];
uniform vec4   lightDiffuseColour[2];
uniform vec4   ambient;

attribute vec4 vertex;
attribute vec3 normal;
attribute vec4 blendIndices;
attribute vec4 blendWeights;
attribute vec4 uv0;

void main()
{	
	mat2x4 blendDQ = blendTwoWeightsAntipod(blendWeights, blendIndices, worldDualQuaternion2x4Array);

	float len = length(blendDQ[0]);
	blendDQ /= len;

	vec3 blendPosition = calculateBlendPosition(vertex.xyz, blendDQ);
		
	//No need to normalize, the magnitude of the normal is preserved because only rotation is performed
	vec3 blendNormal = calculateBlendNormal(normal, blendDQ);
	
	gl_Position =  viewProjectionMatrix * vec4(blendPosition, 1.0);
	
	// Lighting - support point and directional
	vec3 lightDir0 = normalize(lightPos[0].xyz - (blendPosition * lightPos[0].w));
	vec3 lightDir1 = normalize(lightPos[1].xyz - (blendPosition * lightPos[1].w));

	gl_TexCoord[0] = uv0;

	gl_FrontColor = gl_FrontMaterial.diffuse * (ambient + (clamp(dot(lightDir0, blendNormal), 0.0, 1.0) * lightDiffuseColour[0]) + 
		(clamp(dot(lightDir1, blendNormal), 0.0, 1.0) * lightDiffuseColour[1]));			
}

