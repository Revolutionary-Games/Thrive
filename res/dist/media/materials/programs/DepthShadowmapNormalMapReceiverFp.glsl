uniform float inverseShadowmapSize;
uniform float fixedDepthBias;
uniform float gradientClamp;
uniform float gradientScaleBias;
uniform float shadowFuzzyWidth;
uniform vec4 lightColour;

uniform sampler2D shadowMap;
uniform sampler2D normalMap;

varying vec3 tangentLightDir;


// Expand a range-compressed vector
vec3 expand(vec3 v)
{
	return (v - 0.5) * 2.0;
}

void main()
{

	// get the new normal and diffuse values
	vec3 normal = normalize(expand(texture2D(normalMap, gl_TexCoord[1].xy).xyz));
	
	vec4 vertexColour = clamp(dot(normal, tangentLightDir),0.0,1.0) * lightColour;


	vec4 shadowUV = gl_TexCoord[0];
	// point on shadowmap
#if LINEAR_RANGE
	shadowUV.xy = shadowUV.xy / shadowUV.w;
#else
	shadowUV = shadowUV / shadowUV.w;
#endif
	float centerdepth = texture2D(shadowMap, shadowUV.xy).x;
    
    // gradient calculation
  	float pixeloffset = inverseShadowmapSize;
    vec4 depths = vec4(
    	texture2D(shadowMap, shadowUV.xy + vec2(-pixeloffset, 0)).x,
    	texture2D(shadowMap, shadowUV.xy + vec2(+pixeloffset, 0)).x,
    	texture2D(shadowMap, shadowUV.xy + vec2(0, -pixeloffset)).x,
    	texture2D(shadowMap, shadowUV.xy + vec2(0, +pixeloffset)).x);

	vec2 differences = abs( depths.yw - depths.xz );
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

	gl_FragColor = vertexColour * visibility;
#else
	// hard test
#if PCF
	// use depths from prev, calculate diff
	depths += depthAdjust;
	float final = (finalCenterDepth > shadowUV.z) ? 1.0 : 0.0;
	final += (depths.x > shadowUV.z) ? 1.0 : 0.0;
	final += (depths.y > shadowUV.z) ? 1.0 : 0.0;
	final += (depths.z > shadowUV.z) ? 1.0 : 0.0;
	final += (depths.w > shadowUV.z) ? 1.0 : 0.0;
	
	final *= 0.2;

	gl_FragColor = vec4(vertexColour.xyz * final, 1);
	
#else
	gl_FragColor = (finalCenterDepth > shadowUV.z) ? vertexColour : vec4(0,0,0,1);
#endif

#endif
   
}

