shader_type spatial;
render_mode unshaded, cull_disabled;

uniform sampler2D mainTexture;
uniform sampler2D projected;
uniform sampler2D dirt;

uniform mat4[500] projectionMatrices;

uniform sampler2D blendCurve;

uniform vec2 mainTextureScaleTop;
uniform vec2 mainTextureScaleSide;
uniform vec2 mainTextureScaleFront;

uniform vec3 projectionScale;

varying vec3 pos;
varying vec3 normal;

void vertex() {
	// Called for every vertex the material is visible on.
	mat4 staticViewInverse = INV_VIEW_MATRIX;
	staticViewInverse[3] = vec4(0.0f);
	normal = (NORMAL).xyz;
	pos = VERTEX;
	VERTEX = vec3(vec2(UV.x, UV.y) - 0.5f, 0.0f);
}

void fragment() {
	// Called for every pixel the material is visible on.
	ALBEDO = vec3(0.0f);
	
	vec4 top = texture(mainTexture, vec2(pos.x, pos.z) * mainTextureScaleTop);
	vec4 side = texture(mainTexture, vec2(pos.z, pos.y) * mainTextureScaleSide);
	vec4 front = texture(mainTexture, vec2(pos.x, pos.y) * mainTextureScaleFront);
	
	vec4 dirtTop = texture(dirt, vec2(pos.x, pos.z) * mainTextureScaleTop);
	vec4 dirtSide = texture(dirt, vec2(pos.z, pos.y) * mainTextureScaleSide);
	vec4 dirtFront = texture(dirt, vec2(pos.x, pos.y) * mainTextureScaleFront);
	
	float finalTopNormal = abs(normal.y);
	float finalFrontNormal = abs(normal.z);
	
	vec4 finalMix = mix(
		mix(side, top, texture(blendCurve, vec2(finalTopNormal, 0.0f))),
		mix(front, top, texture(blendCurve, vec2(finalTopNormal, 0.0f))),
		texture(blendCurve, vec2(finalFrontNormal, 0.0f)));
	
	vec4 dirtMix = mix(
		mix(dirtSide, dirtTop, texture(blendCurve, vec2(finalTopNormal, 0.0f))),
		mix(dirtFront, dirtTop, texture(blendCurve, vec2(finalTopNormal, 0.0f))),
		texture(blendCurve, vec2(finalFrontNormal, 0.0f)));
		
	/*ALBEDO = vec3(
		texture(patternCurveR, vec2(finalMix.r, 0.0f)).r,
		texture(patternCurveG, vec2(finalMix.g, 0.0f)).g,
		texture(patternCurveB, vec2(finalMix.b, 0.0f)).b
		);*/
		
	finalMix = clamp(finalMix, 0.0f, 1.0f);
	ALBEDO = finalMix.rgb * dirtMix.rgb * COLOR.rgb;
	
	for (int i = 0; i < projectionMatrices.length(); i++)
	{
		mat4 matrix = projectionMatrices[i];
		
		vec3 posProjected = (matrix * vec4(pos, 1.0f)).xyz * projectionScale;
		
		matrix[3] = vec4(0.0f, 0.0f, 0.0f, 1.0f);
		
		vec3 normalProjected = (matrix * vec4(normal, 1.0f)).xyz;
		
		if (normalProjected.z < 0.2f)
			continue;
			
		if (posProjected.z > 1.0f || posProjected.z < -0.8f)
			continue;
		
		vec4 color = texture(projected, posProjected.xy);
		
		ALBEDO = mix(ALBEDO, color.rgb * dirtMix.rgb, color.a) * COLOR.rgb;
	}
}
