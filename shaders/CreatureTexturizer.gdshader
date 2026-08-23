shader_type spatial;
render_mode unshaded, cull_disabled;

uniform sampler2D mainTexture;
uniform sampler2D projected1;
uniform sampler2D projected2;
uniform sampler2D dirt;
uniform mat4[500] matrices;

uniform sampler2D blendCurve;

uniform sampler2D patternCurveR;
uniform sampler2D patternCurveG;
uniform sampler2D patternCurveB;

uniform sampler2D seamTexture;

uniform vec2 noiseScaleTop;
uniform vec2 noiseScaleSide;
uniform vec2 noiseScaleFront;

uniform vec3 projected2Scale;

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
	
	vec4 top = texture(projected1, vec2(pos.x, pos.z) * noiseScaleTop);
	vec4 side = texture(projected1, vec2(pos.z, pos.y) * noiseScaleSide);
	vec4 front = texture(projected1, vec2(pos.x, pos.y) * noiseScaleFront);
	
	vec4 dirtTop = texture(dirt, vec2(pos.x, pos.z) * noiseScaleTop);
	vec4 dirtSide = texture(dirt, vec2(pos.z, pos.y) * noiseScaleSide);
	vec4 dirtFront = texture(dirt, vec2(pos.x, pos.y) * noiseScaleFront);
	
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
	
	finalMix = clamp(finalMix, 0.0f, 1.0f);
		
	/*ALBEDO = vec3(
		texture(patternCurveR, vec2(finalMix.r, 0.0f)).r,
		texture(patternCurveG, vec2(finalMix.g, 0.0f)).g,
		texture(patternCurveB, vec2(finalMix.b, 0.0f)).b
		);*/
	
	vec4 seam = vec4(0.0f);
	
	if (abs(normal.y) > 0.2f && abs(normal.y) < 0.8f)
	{
		float height = (normal.y - 0.2f) / 0.6f;
		// seam = texture(seamTexture, vec2(pos.z * (-abs(pos.x) + 0.2f), height));
	}
		
	ALBEDO = mix(finalMix.rgb, seam.rgb, seam.a) * dirtMix.rgb * COLOR.rgb;
	
	for (int i = 0; i < matrices.length(); i++)
	{
		mat4 matrix = matrices[i];
		
		vec3 pos1 = (matrix * vec4(pos, 1.0f)).xyz * projected2Scale;
		
		matrix[3] = vec4(0.0f, 0.0f, 0.0f, 1.0f);
		
		vec3 normal1 = (matrix * vec4(normal, 1.0f)).xyz;
		
		if (normal1.z < 0.2f)
			continue;
			
		if (pos1.x < 0.0f || pos1.y < 0.0f || pos1.x > 1.0f || pos1.y > 1.0f
			|| pos1.z > 1.0f || pos1.z < -0.8f)
			continue;
		
		vec4 color = texture(projected2, pos1.xy);
		
		ALBEDO = mix(ALBEDO, color.rgb * dirtMix.rgb, color.a) * COLOR.rgb;
	}
}
