// Example GLSL program for skinning with two bone weights per vertex

attribute vec4 vertex;
attribute vec3 normal;
attribute vec4 uv0;
attribute vec4 blendIndices;
attribute vec4 blendWeights;

// 3x4 matrix, passed as vec4's for compatibility with GL 2.0
// GL 2.0 supports 3x4 matrices
// Support 24 bones ie 24*3, but use 72 since our parser can pick that out for sizing
uniform vec4 worldMatrix3x4Array[72];
uniform mat4 viewProjectionMatrix;
uniform vec4 lightPos[2];
uniform vec4 lightDiffuseColour[2];
uniform vec4 ambient;
uniform vec4 diffuse;

void main()
{
	vec3 blendPos = vec3(0.0, 0.0, 0.0);
	vec3 blendNorm = vec3(0.0, 0.0, 0.0);
	
	for (int bone = 0; bone < 2; ++bone)
	{
		// perform matrix multiplication manually since no 3x4 matrices
        // ATI GLSL compiler can't handle indexing an array within an array so calculate the inner index first
		int idx = int(blendIndices[bone]) * 3;
        // ATI GLSL compiler can't handle unrolling the loop so do it manually
        // ATI GLSL has better performance when mat4 is used rather than using individual dot product
        // There is a bug in ATI mat4 constructor (Cat 7.2) when indexed uniform array elements are used as vec4 parameter so manually assign
		mat4 worldMatrix;
		worldMatrix[0] = worldMatrix3x4Array[idx];
		worldMatrix[1] = worldMatrix3x4Array[idx + 1];
		worldMatrix[2] = worldMatrix3x4Array[idx + 2];
		worldMatrix[3] = vec4(0);
		// now weight this into final 
		float weight = blendWeights[bone];
		blendPos += (vertex * worldMatrix).xyz * weight;
		
		mat3 worldRotMatrix = mat3(worldMatrix[0].xyz, worldMatrix[1].xyz, worldMatrix[2].xyz);
		blendNorm += (normal * worldRotMatrix) * weight;
	}

	blendNorm = normalize(blendNorm);

	// apply view / projection to position
	gl_Position = viewProjectionMatrix * vec4(blendPos, 1.0);

	// simple vertex lighting model
	vec3 lightDir0 = normalize(
		lightPos[0].xyz -  (blendPos * lightPos[0].w));
	vec3 lightDir1 = normalize(
		lightPos[1].xyz -  (blendPos * lightPos[1].w));
		
	gl_FrontColor = diffuse * (ambient + (clamp(dot(lightDir0, blendNorm), 0.0, 1.0) * lightDiffuseColour[0]) + 
		(clamp(dot(lightDir1, blendNorm), 0.0, 1.0) * lightDiffuseColour[1]));	

	gl_TexCoord[0] = uv0;
	
}
