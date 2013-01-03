#version 120

mat2x4 blendTwoWeightsAntipod(vec4 blendWgt, vec4 blendIdx, vec4 dualQuaternions[24]);
vec3 calculateBlendPosition(vec3 position, mat2x4 blendDQ);

uniform vec4 worldDualQuaternion2x4Array[24];
uniform mat4x4 viewProjectionMatrix;
uniform vec4   ambient;

attribute vec4 vertex;
attribute vec4 blendIndices;
attribute vec4 blendWeights;

//Shadow caster pass
void main()
{
	mat2x4 blendDQ = blendTwoWeightsAntipod(blendWeights, blendIndices, worldDualQuaternion2x4Array);

	float len = length(blendDQ[0]);
	blendDQ /= len;

	vec3 blendPosition = calculateBlendPosition(vertex.xyz, blendDQ);

	// view / projection
	gl_Position = viewProjectionMatrix * vec4(blendPosition, 1.0);
	
	gl_FrontColor = ambient;
}

