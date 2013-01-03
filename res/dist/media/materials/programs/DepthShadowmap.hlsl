/* This file implements standard programs for depth shadow mapping. 
   These particular ones are suitable for additive lighting models, and
   include 3 techniques to reduce depth fighting on self-shadowed surfaces,
   constant bias, gradient (slope-scale) bias, and a fuzzy shadow map comparison*/


// Shadow caster vertex program.
void casterVP(
	float4 position			: POSITION,
	out float4 outPos		: POSITION,
	out float2 outDepth		: TEXCOORD0,

	uniform float4x4 worldViewProj,
	uniform float4 texelOffsets,
	uniform float4 depthRange
	)
{
	outPos = mul(worldViewProj, position);

	// fix pixel / texel alignment
	outPos.xy += texelOffsets.zw * outPos.w;
	// linear depth storage
	// offset / scale range output
#if LINEAR_RANGE
	outDepth.x = (outPos.z - depthRange.x) * depthRange.w;
#else
	outDepth.x = outPos.z;
#endif
	outDepth.y = outPos.w;
}


// Shadow caster fragment program for high-precision single-channel textures	
void casterFP(
	float2 depth			: TEXCOORD0,
	out float4 result		: COLOR)
	
{
#if LINEAR_RANGE
	float finalDepth = depth.x;
#else
	float finalDepth = depth.x / depth.y;
#endif
	// just smear across all components 
	// therefore this one needs high individual channel precision
	result = float4(finalDepth, finalDepth, finalDepth, 1);
}


	
void receiverVP(
	float4 position		: POSITION,
	float4 normal		: NORMAL,

	out float4 outPos			: POSITION,
	out float4 outColour		: COLOR,
	out float4 outShadowUV		: TEXCOORD0,

	uniform float4x4 world,
	uniform float4x4 worldIT,
	uniform float4x4 worldViewProj,
	uniform float4x4 texViewProj,
	uniform float4 lightPosition,
	uniform float4 lightColour,
	uniform float4 shadowDepthRange
	)
{
	float4 worldPos = mul(world, position);
	outPos = mul(worldViewProj, position);

	float3 worldNorm = mul(worldIT, normal).xyz;

	// calculate lighting (simple vertex lighting)
	float3 lightDir = normalize(
		lightPosition.xyz -  (worldPos.xyz * lightPosition.w));

	outColour = lightColour * max(dot(lightDir, worldNorm), 0.0);

	// calculate shadow map coords
	outShadowUV = mul(texViewProj, worldPos);
#if LINEAR_RANGE
	// adjust by fixed depth bias, rescale into range
	outShadowUV.z = (outShadowUV.z - shadowDepthRange.x) * shadowDepthRange.w;
#endif
	


	
}

void receiverFP(
	float4 position			: POSITION,
	float4 shadowUV			: TEXCOORD0,
	float4 vertexColour		: COLOR,

	uniform sampler2D shadowMap : register(s0),
	uniform float inverseShadowmapSize,
	uniform float fixedDepthBias,
	uniform float gradientClamp,
	uniform float gradientScaleBias,
	uniform float shadowFuzzyWidth,
	
	out float4 result		: COLOR)
{
	// point on shadowmap
#if LINEAR_RANGE
	shadowUV.xy = shadowUV.xy / shadowUV.w;
#else
	shadowUV = shadowUV / shadowUV.w;
#endif
	float centerdepth = tex2D(shadowMap, shadowUV.xy).x;
    
    // gradient calculation
  	float pixeloffset = inverseShadowmapSize;
    float4 depths = float4(
    	tex2D(shadowMap, shadowUV.xy + float2(-pixeloffset, 0)).x,
    	tex2D(shadowMap, shadowUV.xy + float2(+pixeloffset, 0)).x,
    	tex2D(shadowMap, shadowUV.xy + float2(0, -pixeloffset)).x,
    	tex2D(shadowMap, shadowUV.xy + float2(0, +pixeloffset)).x);

	float2 differences = abs( depths.yw - depths.xz );
	float gradient = min(gradientClamp, max(differences.x, differences.y));
	float gradientFactor = gradient * gradientScaleBias;

	// visibility function
	float depthAdjust = gradientFactor + (fixedDepthBias * centerdepth);
	float finalCenterDepth = centerdepth + depthAdjust;

	// shadowUV.z contains lightspace position of current object

#if FUZZY_TEST
	// fuzzy test - introduces some ghosting in result and doesn't appear to be needed?
	//float visibility = saturate(1 + delta_z / (gradient * shadowFuzzyWidth));
	float visibility = saturate(1 + (finalCenterDepth - shadowUV.z) * shadowFuzzyWidth * shadowUV.w);

	result = vertexColour * visibility;
#else
	// hard test
#if PCF
	// use depths from prev, calculate diff
	depths += depthAdjust.xxxx;
	float final = (finalCenterDepth > shadowUV.z) ? 1.0f : 0.0f;
	final += (depths.x > shadowUV.z) ? 1.0f : 0.0f;
	final += (depths.y > shadowUV.z) ? 1.0f : 0.0f;
	final += (depths.z > shadowUV.z) ? 1.0f : 0.0f;
	final += (depths.w > shadowUV.z) ? 1.0f : 0.0f;
	
	final *= 0.2f;

	result = float4(vertexColour.xyz * final, 1);
	
#else
	result = (finalCenterDepth > shadowUV.z) ? vertexColour : float4(0,0,0,1);
#endif

#endif
   

	
}




// Expand a range-compressed vector
float3 expand(float3 v)
{
	return (v - 0.5) * 2;
}


/* Normal mapping plus depth shadowmapping receiver programs
*/
void normalMapShadowReceiverVp(float4 position	: POSITION,
			 float3 normal		: NORMAL,
			 float2 uv			: TEXCOORD0,
			 float3 tangent     : TANGENT0,
			 
			 // outputs
			 out float4 outPos    	 : POSITION,
			 out float4 outShadowUV	 : TEXCOORD0,
			 out float2 oUv	 		 : TEXCOORD1,
			 out float3 oTSLightDir  : TEXCOORD2,
			 // parameters
			 uniform float4 lightPosition, // object space
			 uniform float4x4 world,
			 uniform float4x4 worldViewProj,
			 uniform float4x4 texViewProj)
{
	float4 worldPos = mul(world, position);
	outPos = mul(worldViewProj, position);

	// calculate shadow map coords
	outShadowUV = mul(texViewProj, worldPos);
#if LINEAR_RANGE
	// adjust by fixed depth bias, rescale into range
	outShadowUV.z = (outShadowUV.z - shadowDepthRange.x) * shadowDepthRange.w;
#endif
	
	// pass the main uvs straight through unchanged
	oUv = uv;

	// calculate tangent space light vector
	// Get object space light direction
	// Non-normalised since we'll do that in the fragment program anyway
	float3 lightDir = lightPosition.xyz -  (position * lightPosition.w);

	// Calculate the binormal (NB we assume both normal and tangent are
	// already normalised)
	// NB looks like nvidia cross params are BACKWARDS to what you'd expect
	// this equates to NxT, not TxN
	float3 binormal = cross(tangent, normal);
	
	// Form a rotation matrix out of the vectors
	float3x3 rotation = float3x3(tangent, binormal, normal);
	
	// Transform the light vector according to this matrix
	oTSLightDir = mul(rotation, lightDir);

	
}


void normalMapShadowReceiverFp(
			  float4 shadowUV	: TEXCOORD0,
			  float2 uv			: TEXCOORD1,
			  float3 TSlightDir : TEXCOORD2,

			  out float4 result	: COLOR,

			  uniform float4 lightColour,
			  uniform float inverseShadowmapSize,
			  uniform float fixedDepthBias,
			  uniform float gradientClamp,
			  uniform float gradientScaleBias,
			  uniform float shadowFuzzyWidth,
			  
			  uniform sampler2D   shadowMap : register(s0),
			  uniform sampler2D   normalMap : register(s1),
			  uniform samplerCUBE normalCubeMap : register(s2))
{

	// retrieve normalised light vector, expand from range-compressed
	float3 lightVec = expand(texCUBE(normalCubeMap, TSlightDir).xyz);

	// get bump map vector, again expand from range-compressed
	float3 bumpVec = expand(tex2D(normalMap, uv).xyz);

	// Calculate dot product
	float4 vertexColour = lightColour * dot(bumpVec, lightVec);


	// point on shadowmap
#if LINEAR_RANGE
	shadowUV.xy = shadowUV.xy / shadowUV.w;
#else
	shadowUV = shadowUV / shadowUV.w;
#endif
	float centerdepth = tex2D(shadowMap, shadowUV.xy).x;
    
    // gradient calculation
  	float pixeloffset = inverseShadowmapSize;
    float4 depths = float4(
    	tex2D(shadowMap, shadowUV.xy + float2(-pixeloffset, 0)).x,
    	tex2D(shadowMap, shadowUV.xy + float2(+pixeloffset, 0)).x,
    	tex2D(shadowMap, shadowUV.xy + float2(0, -pixeloffset)).x,
    	tex2D(shadowMap, shadowUV.xy + float2(0, +pixeloffset)).x);

	float2 differences = abs( depths.yw - depths.xz );
	float gradient = min(gradientClamp, max(differences.x, differences.y));
	float gradientFactor = gradient * gradientScaleBias;

	// visibility function
	float depthAdjust = gradientFactor + (fixedDepthBias * centerdepth);
	float finalCenterDepth = centerdepth + depthAdjust;

	// shadowUV.z contains lightspace position of current object

#if FUZZY_TEST
	// fuzzy test - introduces some ghosting in result and doesn't appear to be needed?
	//float visibility = saturate(1 + delta_z / (gradient * shadowFuzzyWidth));
	float visibility = saturate(1 + (finalCenterDepth - shadowUV.z) * shadowFuzzyWidth * shadowUV.w);

	result = vertexColour * visibility;
#else
	// hard test
#if PCF
	// use depths from prev, calculate diff
	depths += depthAdjust.xxxx;
	float final = (finalCenterDepth > shadowUV.z) ? 1.0f : 0.0f;
	final += (depths.x > shadowUV.z) ? 1.0f : 0.0f;
	final += (depths.y > shadowUV.z) ? 1.0f : 0.0f;
	final += (depths.z > shadowUV.z) ? 1.0f : 0.0f;
	final += (depths.w > shadowUV.z) ? 1.0f : 0.0f;
	
	final *= 0.2f;

	result = float4(vertexColour.xyz * final, 1);
	
#else
	result = (finalCenterDepth > shadowUV.z) ? vertexColour : float4(0,0,0,1);
#endif

#endif


	
}

